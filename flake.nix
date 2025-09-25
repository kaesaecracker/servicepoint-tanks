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
        {
          frontend = pkgs.mkShell {
            inputsFrom = [ selfPkgs.servicepoint-tanks-frontend ];
          };
          default = import ./shell.nix {
            inherit pkgs lib;
          };
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
        }
      );

      formatter = forAllSystems ({ pkgs, ... }: pkgs.nixfmt-tree);
    };
}
