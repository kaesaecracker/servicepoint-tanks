{
  description = "Dev shell flake for servicepoint-tanks";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-25.05";
  };

  outputs =
    { self, nixpkgs }:
    let
      supported-systems = [
        "x86_64-linux"
        "aarch64-linux"
      ];
      forAllSystems =
        f:
        nixpkgs.lib.genAttrs supported-systems (
          system:
          f rec {
            pkgs = nixpkgs.legacyPackages.${system};
            lib = nixpkgs.lib;
            inherit system;
            selfPkgs = self.packages.${system};
          }
        );
    in
    {
      devShells = forAllSystems (
        {
          pkgs,
          lib,
          selfPkgs,
          ...
        }:
        let
          frontend-set = {
            inputsFrom = [ selfPkgs.servicepoint-tanks-frontend ];
          };
          backend-set = {
            inputsFrom = [ selfPkgs.servicepoint-tanks-backend ];
            packages = with pkgs; [
              nuget-to-json
              cargo-tarpaulin
            ];
          };
        in
        {
          frontend = pkgs.mkShell frontend-set;
          backend = pkgs.mkShell backend-set;
          default = pkgs.mkShell (frontend-set // backend-set);
        }
      );

      packages = forAllSystems (
        { pkgs, lib, ... }:
        {
          servicepoint-tanks-frontend = pkgs.buildNpmPackage (finalAttrs: {
            pname = "tank-frontend";
            version = "0.0.0";

            src = ./tank-frontend;
            npmDepsHash = "sha256-HvwoSeKHBDkM/5OHDkgSOxfHx1gbnKif/3QfDb6r5mE=";

            installPhase = ''
              cp -rv dist/ $out
            '';
          });

          servicepoint-tanks-backend = pkgs.buildDotnetModule {
            pname = "servicepoint-tanks-backend";
            version = "0.0.0";

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;

            src = ./tanks-backend;
            projectFile = "TanksServer.sln";
            nugetDeps = ./tanks-backend/deps.json;
            selfContainedBuild = true;

            nativeBuildInputs = with pkgs; [
              pkg-config
              xe
              xz
              gnumake
              iconv
            ];
          };
        }
      );

      formatter = forAllSystems ({ pkgs, ... }: pkgs.nixfmt-tree);
    };
}
