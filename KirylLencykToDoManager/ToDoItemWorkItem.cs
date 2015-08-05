using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KirylLencykToDoManager.ToDoManagerServiceReference;
using ToDo.Infrastructure;

namespace KirylLencykToDoManager
{
    public enum ToDoWorkType
    {
        Add, Remove, Update
    }

    public class ToDoItemWorkItem
    {
        public ToDoWorkType WorkType { get; set; }

        public ToDoItem Item { get; set; }

    }
}
