# To learn more about how to use Nix to configure your environment
# see: https://developers.google.com/idx/guides/customize-idx-env
{ pkgs, ... }: {
  # Which nixpkgs channel to use.
  channel = "stable-24.11"; # or "unstable"
  # Use https://search.nixos.org/packages to find packages
  packages = [
    pkgs.nodejs_22
    pkgs.uv        # Gestor de paquetes Python — requerido por specify-cli (GitHub Spec Kit / SDD)
  ];
  # Sets environment variables in the workspace
  env = {};
  idx = {
    # Search for the extensions you want on https://open-vsx.org/ and use "publisher.id"
    extensions = [
      "angular.ng-template"
      "google.gemini-cli-vscode-ide-companion"
    ];
    workspace = {
      # Runs when a workspace is first created with this `dev.nix` file
      onCreate = {
        npm-install = "npm i --no-audit --no-progress --timing";
        # Instala specify-cli (GitHub Spec Kit) para la metodología SDD
        specify-install = "uv tool install specify-cli --from git+https://github.com/github/spec-kit.git";
        # Open editors for the following files by default, if they exist:
        default.openFiles = [ "src/app/app.component.ts" ];
      };
      # To run something each time the workspace is (re)started, use the `onStart` hook
    };
    # Enable previews and customize configuration
    previews = {
      enable = true;
      previews = {
        web = {
          command = ["npm" "run" "start" "--" "--port" "$PORT" "--host" "0.0.0.0"];
          manager = "web";
        };
      };
    };
  };
}
