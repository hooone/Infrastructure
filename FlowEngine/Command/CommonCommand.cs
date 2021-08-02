using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Command
{
    public class CommonCommand : ICommand
    {
        public static CommonCommand NewCommand()
        {
            CommonCommand rst = new CommonCommand();
            rst.Id = Guid.NewGuid().ToString("N");
            rst.Name = "流程步骤";
            rst.Pre = new Precondition();
            rst.Pre.Id = Guid.NewGuid().ToString("N");
            rst.Pre.Seq = 1;
            rst.Pre.CommandId = rst.Id;
            rst.Post = new Postcondition();
            rst.Post.Id = Guid.NewGuid().ToString("N");
            rst.Post.Seq = 2;
            rst.Post.CommandId = rst.Id;
            return rst;
        }
        public string Id { get; set; }
        public string Name { get; set; }
        public Precondition Pre { get; set; } 
        public Postcondition Post { get; set; }

        public bool Init()
        {
            return true;
        }

        public bool Execute()
        {
            return true;
        }

        public List<Postcondition> GetPostcondition()
        {
            return new List<Postcondition>() { Post };
        }

        public List<Precondition> GetPrecondition()
        {
            return new List<Precondition>() { Pre };
        }

    }
}
