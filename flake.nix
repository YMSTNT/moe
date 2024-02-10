{
  description = "A multi-purpose Discord bot made using Discord.Net.";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachSystem [ "x86_64-linux" "aarch64-linux" ] (system:
      let
        pkgs = import nixpkgs { inherit system; };
        version = builtins.substring 0 8 self.lastModifiedDate or "dirty";
      in
      {
        packages.moe = pkgs.callPackage ./nix/package.nix { inherit version; };
        defaultPackage = self.packages.${system}.moe;
      }
    );
}
