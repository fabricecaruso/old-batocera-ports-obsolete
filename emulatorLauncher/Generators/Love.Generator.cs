using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace emulatorLauncher
{
    class LoveGenerator : Generator
    {
        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            string path = AppConfig.GetFullPath("love");

            string exe = Path.Combine(path, "love.exe");
            if (!File.Exists(exe))
                return null;
				
				
			if (Path.GetExtension(rom).ToLower() == ".exe")
			{
				path = Path.GetDirectoryName(rom);
				
				if (!File.Exists(rom))
                return null;

                return new ProcessStartInfo()
                {
					FileName = rom,
					WorkingDirectory = path
                };

			}
			
			commandArray.Add('\u0022' + rom + '\u0022');

			string args = string.Join(" ", commandArray);
            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = path,
                Arguments = args,
            };
        }
    }
}
