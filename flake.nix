{
  description = "flake for servicepoint-tanks";

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
        fn:
        nixpkgs.lib.genAttrs supported-systems (
          system:
          fn {
            inherit system;
            inherit (nixpkgs) lib;
            pkgs = nixpkgs.legacyPackages.${system};
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
            packages = with pkgs; [
              typescript
              nodejs
            ];
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
        {
          pkgs,
          lib,
          selfPkgs,
          ...
        }:
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

          servicepoint-binding-csharp = pkgs.buildDotnetModule {
            pname = "servicepoint-binding-csharp";
            version = "0.0.0";

            src = ./tanks-backend/servicepoint-binding-csharp;
            projectFile = "ServicePoint/ServicePoint.csproj";
            nugetDeps = ./tanks-backend/deps.json;

            packNupkg = true;

            nativeBuildInputs = with pkgs; [
              pkg-config
              xe
              xz
              gnumake
              iconv

              (pkgs.symlinkJoin {
                name = "rust-toolchain";
                paths = with pkgs; [
                  rustc
                  cargo
                  rustPlatform.rustcSrc
                  rustfmt
                  clippy
                ];
              })
            ];
          };

          servicepoint-tanks-backend = pkgs.buildDotnetModule {
            pname = "servicepoint-tanks-backend";
            version = "0.0.0";

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;

            src = ./tanks-backend;
            projectFile =   "TanksServer/TanksServer.csproj";
            nugetDeps = ./tanks-backend/deps.json;

            selfContainedBuild = true;

            buildInputs = [ selfPkgs.servicepoint-binding-csharp ];

            nativeBuildInputs = with pkgs; [
              # todo needed?
              gnumake
              iconv
            ];
          };
        }
      );

      formatter = forAllSystems ({ pkgs, ... }: pkgs.nixfmt-tree);
    };
}
