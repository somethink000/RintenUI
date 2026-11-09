global using Rinten;
global using Rinten.Diagnostics;
global using Rinten.Tasks;
global using Rinten.UI;
global using static Rinten.Internal.GlobalSystemNamespace;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
using System.Runtime.CompilerServices;

//The engine draws the panels and reaches inside them to do it; the rest are the engine's own friends
[assembly: InternalsVisibleTo( "Rinten.Engine" )]
[assembly: InternalsVisibleTo( "Rinten.UI.Native" )]
[assembly: InternalsVisibleTo( "Rinten.Test.Unit" )]
[assembly: InternalsVisibleTo( "Rinten.Test.Engine" )]
[assembly: InternalsVisibleTo( "Rinten.Test.Integration" )]
[assembly: InternalsVisibleTo( "Benchmark" )]
[assembly: InternalsVisibleTo( "Rinten.GameInstance" )]
[assembly: InternalsVisibleTo( "Rinten.Tools" )]
[assembly: InternalsVisibleTo( "Rinten.Code" )]
[assembly: InternalsVisibleTo( "Rinten.Porting.SBox" )]
[assembly: InternalsVisibleTo( "Rinten.AppSystem" )]
[assembly: InternalsVisibleTo( "MenuBuild" )]
[assembly: InternalsVisibleTo( "Rinten-Server" )]
[assembly: InternalsVisibleTo( "Rinten" )]
[assembly: InternalsVisibleTo( "Rinten-Standalone" )]
[assembly: InternalsVisibleTo( "Rinten-Web" )]
[assembly: InternalsVisibleTo( "Rinten-Android" )]
[assembly: InternalsVisibleTo( "CreateGameCache" )]
[assembly: InternalsVisibleTo( "McpSnippet" )]
