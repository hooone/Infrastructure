using FlowEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEngine.Command
{
    public abstract class NoBranchBaseCommand<T> : BaseCommand<T> where T : new()
    {
        protected Postcondition Post;

        public sealed override List<ConditionModel> GetConditions()
        {
            var pre = new ConditionModel();
            pre.Seq = 1;
            var post = new ConditionModel();
            post.Seq = 2;
            return new List<ConditionModel>() { pre, post };
        }

        public sealed override void RegisterLink(List<LinkViewModel> links)
        {
            if (Post == null)
                Post = new Postcondition();
            foreach (var item in links)
            {
                Post.RegisterDest(item.DestCondition);
            }
        }
    }
}
