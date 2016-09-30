
namespace erg.compiler
{

    public class CompilerResult {
        public bool Success = false;
        // TODO warning
        // TODO errors
        // TODO others
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
