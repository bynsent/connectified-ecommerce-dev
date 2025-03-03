using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.WebJob
{
    public interface IRecurringTasks
    {
        public void RecurringStockUpdate();
        public void RecurringOrderStatusUpdate();
    }
}
