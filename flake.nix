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
        "aarch64-darwin"
        "x86_64-darwin"
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

          servicepoint-tanks-assets = pkgs.stdenvNoCC.mkDerivation {
            pname = "servicepoint-tanks-assets";
            version = "0.0.0";

            src = ./tanks-backend/TanksServer/assets;
            buildPhase = "";
            installPhase = ''
              cp -rv $src $out
            '';
          };

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

            meta = {
              mainProgram = "TanksServer";
            };
          };
        }
      );

      nixosModules.default =
        {
          pkgs,
          config,
          lib,
          ...
        }:
        let
          cfg = config.services.servicepoint-tanks;
          default-user-name = "servicepoint-tanks";
        in
        {
          options.services.servicepoint-tanks = {
            enable = lib.mkEnableOption "servicepoint-tanks";
            package = lib.mkPackageOption pkgs "servicepoint-tanks" { };
            urls = lib.mkOption {
              default = [ "http://localhost:5000" ];
              description = ''
                Configures which protocol to bind on which host:port combination.
              '';
              type = lib.types.listOf lib.types.str;
              example = [
                "http://0.0.0.0"
                "http://localhost:5000"
                # TODO: allow HTTPS
              ];
            };
            user = lib.mkOption {
              default = default-user-name;
              description = ''
                The user under which servicepoint-tanks is run.

                This module utilizes systemd's DynamicUser feature. See the corresponding section in
                {manpage}`systemd.exec(5)` for more details.
              '';
              type = lib.types.str;
            };
            group = lib.mkOption {
              default = default-user-name;
              description = ''
                The group under which servicepoint-tanks is run.

                This module utilizes systemd's DynamicUser feature. See the corresponding section in
                {manpage}`systemd.exec(5)` for more details.
              '';
              type = lib.types.str;
            };
          };

          config = lib.mkIf cfg.enable {
            nixpkgs.overlays = [ self.overlays.default ];

            users = {
              users = lib.mkIf (cfg.user == default-user-name) {
                "${default-user-name}" = {
                  isSystemUser = true;
                  group = cfg.group;
                };
              };

              groups = lib.mkIf (cfg.group == default-user-name) {
                "${default-user-name}" = { };
              };
            };

            systemd.services.sericepoint-tanks = {
              description = "Run the servicepoint-tanks server";
              wantedBy = [ "multi-user.target" ];
              after = [ "network-online.target" ];
              wants = [ "network-online.target" ];

              environment = {
                ASPNETCORE_URLS = "${lib.strings.concatStringsSep ";" cfg.urls}";
              };

              serviceConfig = {
                User = cfg.user;
                Group = cfg.group;
                DynamicUser = true;

                Type = "exec";
                ExecStart = lib.getExe cfg.package;

                # hardening
                NoNewPrivileges = true;
                CapabilityBoundingSet = null;
                SystemCallFilter = [
                  "@system-service"
                  "~@privileged"
                ];
                SystemCallArchitectures = "native";
                AmbientCapabilities = "";
                PrivateMounts = true;
                PrivateUsers = true;
                PrivateTmp = true;
                PrivateDevices = true;
                ProtectHome = true;
                ProtectClock = true;
                ProtectHostname = true;
                ProtectKernelLogs = true;
                ProtectKernelModules = true;
                ProtectKernelTunables = true;
                ProtectProc = "invisible";
                ProtectSystem = "strict";
                ProtectControlGroups = "strict";
                LockPersonality = true;
                RemoveIPC = true;
                RestrictRealtime = true;
                RestrictSUIDSGID = true;
                RestrictNamespaces = true;
                RestrictAddressFamilies = [
                  "AF_INET"
                  "AF_INET6"

                  # TODO: enable unix domain socket bind
                  # "AF_UNIX"
                ];

                # TODO: try fully AOT build with:
                #MemoryDenyWriteExecute = true;
              };
            };
          };

        };

      overlays.default = final: prev: {
        servicepoint-tanks = self.packages."${prev.system}".servicepoint-tanks;
      };

      formatter = forAllSystems ({ pkgs, ... }: pkgs.nixfmt-tree);
    };
}
