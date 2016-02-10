using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using _53230A;

namespace Read3
{
    class Read3
    {
        /*
        * Send a "Read?" command to counter, which triggers a new measurement. The counter will take the number of samples 
        * specified with ":Sample:count", and return them.
        * 
        * Splits the returned batch of readings into separate text-files, named "0.txt" to "<n>.txt"
        *
        * If auxQuery != null, runs this query once per trigger, and logs the result to q.txt
        * 
        * Runs untill terminated with ctrl-c, or timeout
        */

        static void Main(string[] args)
        {

            Ag53230A instr = new Ag53230A();

            StreamWriter[] writers = null;

            string auxQuery = "SYST:TEMP?";
            StreamWriter auxQueryWr = null;

            char[] splitchar = new char[] { ',' };

            while (true) { 
                instr.WriteString("READ?");

                String str = instr.ReadString().TrimEnd();

                String[] readings = str.Split(splitchar);

                // Create readings.Count() streamwriters.
                if (writers == null)
                {
                    writers = new StreamWriter[readings.Count()];

                    for (int i = 0; i < readings.Count(); i++)
                    {
                        writers[i] = new StreamWriter(i.ToString() + ".txt");
                        writers[i].AutoFlush = true;
                    }
                }

                if (readings.Count() != writers.Length)
                    Console.WriteLine("Error! Expected {0} samples, got {1}!", writers.Length, readings.Count());

                // This will break unless every "batch" has the same number of readings.
                for (int i = 0; i < readings.Count(); i++)
                    writers[i].WriteLine(readings[i]);

                // Additional Query
                if(auxQuery != null)
                {
                    if (auxQueryWr == null)
                    {
                        auxQueryWr = new StreamWriter("q.txt");
                        auxQueryWr.AutoFlush = true;
                    }

                    instr.WriteString(auxQuery);
                    auxQueryWr.WriteLine(instr.ReadString().TrimEnd());
                }

            }
        }
    }
}
