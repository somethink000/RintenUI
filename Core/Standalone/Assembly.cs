global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Linq;
global using System.Text;
global using System.Text.Json;
global using static Rinten.Internal.GlobalSystemNamespace;
using System.Runtime.CompilerServices;

//Inside Rinten these sources are Rinten.System, which its own assemblies see into the same way
[assembly: InternalsVisibleTo( "Rinten.Filesystem" )]
[assembly: InternalsVisibleTo( "Rinten.UI" )]
[assembly: InternalsVisibleTo( "Rinten.UI.Native" )]
[assembly: InternalsVisibleTo( "Topten.RichTextKit" )]
[assembly: InternalsVisibleTo( "Rinten.UI.Tests" )]
