using System;

namespace erg.compiler
{

    public class CompilerResult {
        public bool Success = false;
    }
    

    public class Compiler
    {
        public CompilerResult Compile()
        {    
            return new CompilerResult {
                Success = true
            };
        }
    }
}
