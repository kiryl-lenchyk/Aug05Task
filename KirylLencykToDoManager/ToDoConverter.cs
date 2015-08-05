using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KirylLencykToDoManager.ToDoManagerServiceReference;
using ToDo.Infrastructure;

namespace KirylLencykToDoManager
{
    static class ToDoConverter
    {
        public static ToDoItem ToRemoteToDoItem(this IToDoItem toDoItem)
        {
            return new ToDoItem
            {
                IsCompleted = toDoItem.IsCompleted,
                Name = toDoItem.Name,
                ToDoId = toDoItem.ToDoId,
                UserId = toDoItem.UserId
            };
        }
    }
}
