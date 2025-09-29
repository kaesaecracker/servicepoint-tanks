{
  description = "flake for servicepoint-tanks";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-25.05";

    binding = {
      url = "git+https://git.berlin.ccc.de/servicepoint/servicepoint-binding-csharp.git";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    {
      self,
      nixpkgs,
      binding,
    }:
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
            bindingPkgs = binding.packages.${system};
          }
        );
    in
    {
      apps = forAllSystems (
        {
          pkgs,
          lib,
          selfPkgs,
          ...
        }:
        {
          default = {
            type = "app";
            program = "${lib.getBin selfPkgs.servicepoint-tanks}/bin/TanksServer";
          };
        }
      );
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
            inputsFrom = [ selfPkgs.servicepoint-tanks ];
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
          bindingPkgs,
          ...
        }:
        {
          servicepoint-tanks-frontend = pkgs.buildNpmPackage (finalAttrs: {
            pname = "servicepoint-tanks-frontend";
            version = "0.0.0";

            src = ./tank-frontend;
            npmDepsHash = "sha256-HvwoSeKHBDkM/5OHDkgSOxfHx1gbnKif/3QfDb6r5mE=";

            installPhase = ''
              cp -rv dist/ $out
            '';
          });

          servicepoint-tanks-assets = ./tanks-backend/TanksServer/assets;

          servicepoint-tanks = pkgs.buildDotnetModule {
            pname = "servicepoint-tanks";
            version = "0.0.0";

            dotnet-sdk = pkgs.dotnetCorePackages.sdk_8_0;
            dotnet-runtime = pkgs.dotnetCorePackages.runtime_8_0;

            src = ./tanks-backend;
            projectFile = "TanksServer/TanksServer.csproj";
            nugetDeps = ./tanks-backend/deps.json;

            selfContainedBuild = true;

            buildInputs = [ bindingPkgs.servicepoint-binding-csharp ];

            runtimeDeps = [ bindingPkgs.servicepoint-binding-uniffi ];

            makeWrapperArgs = [
              "--set-default TANKSSERVER_CLIENT ${selfPkgs.servicepoint-tanks-frontend}"
              "--set-default TANKSSERVER_ASSETS ${selfPkgs.servicepoint-tanks-assets}"
            ];
          };
        }
      );

      formatter = forAllSystems ({ pkgs, ... }: pkgs.nixfmt-tree);
    };
}
