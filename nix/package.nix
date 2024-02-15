{ buildDotnetModule
, lib
, system
, version
, nuget-packageslock2nix 
}:

buildDotnetModule {
  pname = "moe";
  inherit version;

  src = ../.;

  # doesn't work yet: https://github.com/mdarocha/nuget-packageslock2nix/issues/2
  nugetDeps = nuget-packageslock2nix.lib {
    inherit system;
    lockfiles = [
      ../packages.lock.json
    ];
  };

  projectFile = [ "moe.csproj" ];

  executables = [ "moe" ];

  meta = with lib; {
    description = "A multi-purpose Discord bot made using Discord.Net";
    homepage = "https://github.com/ymstnt/moe/";
    license = licenses.gpl3;
    maintainers = with maintainers; [ gepbird ];
    platforms = platforms.all;
    mainProgram = "moe";
  };
}
