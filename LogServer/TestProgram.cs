using System;
using System.Collections.Generic;
using System.Text;

namespace DLS
{
    class Program
    {
        static void Main(string[] args)
        {
            LogClient ls = new LogClient();
            ls.open("kayrun.cs.rit.edu", 6007);
            string ticket = ls.newTicket();
            ls.addEntry(ticket, "Message 1 start");
            ls.addEntry(ticket, "Message 2");
            ls.addEntry(ticket, "Message 3");
            ls.addEntry(ticket, "Message 4");
            ls.addEntry(ticket, "Message 5");
            ls.addEntry(ticket, "Message 6");
            ls.addEntry(ticket, "Message 7 end");
            List<String> entries = ls.getEntries(ticket);
            for (int i = 0; i < entries.Count; i++)
            {
                Console.WriteLine(entries[i]);
            }
            ls.releaseTicket(ticket);

            Console.WriteLine("Done testing");
            Console.ReadKey();
        }
    }
}