using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTP
{
    class test
    {
        static void Main(string[] args) {
            byte[] byteArr = { 1,2,3};
            BitArray bitArr = new BitArray(byteArr);
            List<byte> rec = convertToByteList(bitArr);
            Console.WriteLine(rec[2].Equals(byteArr[2]));
            Console.WriteLine(bitArr[17]);

            IEnumerator iterator = byteArr.GetEnumerator();
            int i = 0;
            while (iterator.MoveNext()) {
                i++;
               object b = iterator.Current;
            }

            Console.WriteLine(i);
            
            Console.ReadKey();


        }

        static List<byte> convertToByteList(BitArray bitArray)
        {
            byte[] byteArray = new byte[(int)Math.Ceiling((double)bitArray.Length / 8)];
            bitArray.CopyTo(byteArray, 0);
            return new List<byte>(byteArray);
        }
    }
}
