using System.Runtime.CompilerServices;

namespace FilePrep
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await using FileStream fileread = File.OpenRead("C:\\temp\\input.json");
            await using FileStream filewrite = File.Create("C:\\temp\\rates.json");
            using (StreamReader reader = new StreamReader(fileread))
            {
                using (StreamWriter writer = new StreamWriter(filewrite))
                {
                    string? line;
                    writer.WriteLine("[");
                    for(int i = 0; i<9;  i++)
                        line = reader.ReadLine();
                    int j = 0;
                    do
                    {
                        line = reader.ReadLine();
                        if (reader.Peek() != -1)
                        {
                            writer.WriteLine(line);
                        }

                    } while (line != null);
                }
            }
        }
    }
}

