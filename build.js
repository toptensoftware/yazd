var bt = require('./BuildTools/buildTools.js')

// Used for generation version.* files
bt.options.companyName = "Topten Software";

// Load version info
bt.version();

if (bt.options.official)
{
    // Check everything committed
    bt.git_check();

    // Clock version
    bt.clock_version();

    // Force clean
    bt.options.clean = true;
    bt.clean("./Build");
}

// Build
bt.dnbuild("Release", "yaza");
bt.dnbuild("Release", "yazd");

// Pack
bt.dnpack("Release", "yaza", "netcoreapp2.1");
bt.dnpack("Release", "yazd", "netcoreapp2.1");

// Update zips
bt.run("zip", ["yaza.zip", "./Build/Release/yaza/net46/yaza.exe"]);
bt.run("zip", ["yazd.zip", "./Build/Release/yazd/net46/yazd.exe"]);

if (bt.options.official)
{
    // Tag and commit
    bt.git_tag();

    // Push nuget packages
    bt.nupush(`./build/Release/yaza/*.${bt.options.version.build}.nupkg`, `https://api.nuget.org/v3/index.json`);
    bt.nupush(`./build/Release/yazd/*.${bt.options.version.build}.nupkg`, `https://api.nuget.org/v3/index.json`);
}


