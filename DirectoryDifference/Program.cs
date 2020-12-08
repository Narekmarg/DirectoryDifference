using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DirectoryDifference
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "--help")
                {
                    Console.WriteLine("Usage: DirectoryDifference <SkipFileInThisDirectory> <CopyFilesFromThisDirectory> <CopyFileToThisDirectory>");
                    return;
                }
            }
            
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Invalid number of arguments");
                return;
            }
            
            string directoryToCompareWith = args[0];
            string directoryToExtractFrom = args[1];
            string directoryToCopyTo = args[2];
            
            if (!Directory.Exists(directoryToCompareWith))
            {                
                Console.Error.WriteLine("Invalid source");
                return;
            }
            
            if (!Directory.Exists(directoryToExtractFrom))
            {
                Console.Error.WriteLine("Invalid destination");
                return;
            }
            
            string[] extensions = {".docx", ".doc", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf"};
            
            Directory.CreateDirectory(directoryToCopyTo);

            var filesToCopy = 
                new HashSet<HashEntry>(GetFiles(directoryToExtractFrom, extensions).Select(x => new HashEntry(ComputeHash(x), x)));

            filesToCopy.ExceptWith(GetFiles(directoryToCompareWith, extensions).Select(x => new HashEntry(ComputeHash(x), x)));

            foreach (var file in filesToCopy)
            {
                string name = Path.GetFileName(file.FilePath);
                string copyPath = Path.Combine(directoryToCopyTo, name);
                try
                {
                    Console.WriteLine($"Copying file: {file.FilePath}");
                    File.Copy(file.FilePath!, copyPath);
                }
                catch
                {
                    Console.Error.WriteLine($"Copy failed for: {file.FilePath}");
                }
            }
        }


        public static byte[] ComputeHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            return md5.ComputeHash(stream);
        }
        
        
        static IEnumerable<string> GetFiles(string path, string[] extensions) {
            var queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0) {
                path = queue.Dequeue();
                try {
                    foreach (var subDir in Directory.GetDirectories(path)) {
                        queue.Enqueue(subDir);
                    }
                }
                catch(Exception ex) {
                    Console.Error.WriteLine(ex);
                }
                string[] files = null;
                try {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                if (files == null) continue;
                
                foreach (var t in files)
                {
                    if(extensions.Contains(Path.GetExtension(t)))
                        yield return t;
                }
            }
        }
    }

    public class HashEntry : IEquatable<HashEntry>
    {
        private readonly byte[] _hash;

        public HashEntry(byte[] hash, string filePath)
        {
            FilePath = filePath;
            _hash = hash;
        }
        
        public string FilePath { get; }

        public bool Equals(HashEntry other) => 
            _hash.Zip(other!._hash, (x, y) => x == y).All(x => x);

        public override int GetHashCode()
        {
            unchecked
            {
                const int p = 16777619;
                var hash = _hash.Aggregate((int) 2166136261, (current, t) => (current ^ t) * p);

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}