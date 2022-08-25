using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoHoPython.Syntax.Parsing
{
    public sealed class Scanner
    {
        private sealed class FileVisitor
        {
            public readonly string FileName;
            public int Row { get; private set; }
            public int Column { get; private set; }

            private readonly string source;
            private int position;

            public FileVisitor(string fileName)
            {
                FileName = fileName;

                if(!File.Exists(fileName))
                    throw new FileNotFoundException(fileName);
                source = File.ReadAllText(fileName);

                Row = 1;
                Column = 1;
                position = 0;
            }

            public char ScanChar()
            {
                if(position < source.Length)
                    return source[position++];
                return '\0';
            }
        }

        private Stack<FileVisitor> visitorStack;
        private SortedSet<string> visitedFiles;
        private Token LastToken;

        public Scanner(string firstFileToVisit)
        {
            visitorStack = new Stack<FileVisitor>();
            visitedFiles = new SortedSet<string>();

            IncludeFile(firstFileToVisit);
        }

        public void IncludeFile(string fileName)
        {
            visitorStack.Push(new FileVisitor(fileName));
        }

        public void Scan
    }
}
