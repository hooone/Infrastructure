using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Command
{
    public interface ICommand
    {
        string Id { get; set; }
        string Name { get; set; }
        bool Execute();
        List<Precondition> GetPrecondition();
        List<Postcondition> GetPostcondition();
    }
    public class Precondition
    {
        public string Id { get; set; }
        public int Seq { get; set; }
        public string CommandId { get; set; }
    }
    public class Postcondition
    {
        public string Id { get; set; }
        public int Seq { get; set; }
        public string CommandId { get; set; }
    }
}
