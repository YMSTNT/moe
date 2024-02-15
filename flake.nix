{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
    nuget-packageslock2nix = {
      url = "github:mdarocha/nuget-packageslock2nix/main";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs = { self, nixpkgs, flake-utils, nuget-packageslock2nix }:
    {
      nixosModule = import ./nix/module.nix self.outputs.packages;
    } //
    flake-utils.lib.eachSystem [ "x86_64-linux" "aarch64-linux" ] (system:
      let
        pkgs = import nixpkgs { inherit system; };
        version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
      in
      {
        packages.default = pkgs.callPackage ./nix/package.nix { inherit version nuget-packageslock2nix; };
      });
}
