using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HL7
{
    public static class FileSaver
    {
        public static void SavePdfFile(String path, Hl7Message message)
        {
            //UUDecoding here
            byte[] decodedDataBytes = message.GetOriginalBinaryDataPDF();
            
            FileStream pdfFileStream = new FileStream(path, FileMode.Create,FileAccess.ReadWrite);
            using (pdfFileStream)
            {
                BinaryWriter br = new BinaryWriter(pdfFileStream);
                using (br)
                {
                    br.Write(decodedDataBytes);
                    br.Flush();
                }
            }

        }

        public static byte[] UuDecodeData(string data)
        {
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            MemoryStream inputStream = new MemoryStream(dataBytes);
            MemoryStream outputStream = new MemoryStream();

            Codecs.UUDecode(inputStream, outputStream);
            byte[] decodedDataBytes = outputStream.ToArray(); 
            return decodedDataBytes;
        }
    }
}
