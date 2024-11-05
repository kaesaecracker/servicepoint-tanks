{
  description = "Dev shell flake for servicepoint-tanks";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-24.05";
  };

  outputs =
    { self, nixpkgs }:
    let
      lib = nixpkgs.lib;
      forAllSystems = lib.genAttrs lib.systems.flakeExposed;
    in
    {
      devShells = forAllSystems (system: {
        default = import ./shell.nix {
          inherit nixpkgs;
          pkgs = nixpkgs.legacyPackages."${system}";
          lib = nixpkgs.lib;
        };
      });

      formatter = forAllSystems (system: nixpkgs.legacyPackages."${system}".nixfmt-rfc-style);
    };
}
