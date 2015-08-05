using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using KirylLencykToDoManager.ToDoManagerServiceReference;
using ToDo.Infrastructure;

namespace KirylLencykToDoManager
{
    public class ToDoManager : ToDo.Infrastructure.IToDoManager
    {
        private int currentUserId;

        private readonly ToDoManagerServiceReference.ToDoManagerClient toDoManagerClient;

        private List<IToDoItem> localItems; 

        public ToDoManager()
        {
            toDoManagerClient = new ToDoManagerServiceReference.ToDoManagerClient();
        }


        public List<IToDoItem> GetTodoList(int userId)
        {
            if(currentUserId != userId) LoadLocalStorage(userId);

            return localItems;

        }

        public void UpdateToDoItem(IToDoItem todo)
        {
            if (todo.UserId != currentUserId) LoadLocalStorage(todo.UserId);

            IToDoItem toUpdate = localItems.Find(x => x.ToDoId == todo.ToDoId);
            if (toUpdate != null)
            {
                int indexToUpdate = localItems.IndexOf(toUpdate);
                localItems[indexToUpdate] = todo;
                Task.Run(() => UpdateItemAsync(todo));
            }
        }

        public void CreateToDoItem(IToDoItem todo)
        {
            if (todo.UserId != currentUserId) LoadLocalStorage(todo.UserId);

            todo.ToDoId = localItems.Count;
            localItems.Add(todo);
            Task.Run(() => AddItemAsync(todo));
        }

       

        public void DeleteToDoItem(int todoItemId)
        {
            IToDoItem toDelete = localItems.Find(x => x.ToDoId == todoItemId);
            if (toDelete != null)
            {
                localItems.Remove(toDelete);
                Task.Run(() => DeleteItemAsync(toDelete));
            }
        }

        public int CreateUser(string name)
        {
            currentUserId = toDoManagerClient.CreateUser(name);
            LoadLocalStorage(currentUserId);
            return currentUserId;
        }

        private void LoadLocalStorage(int userId)
        {
            localItems = toDoManagerClient.GetTodoList(userId).Cast<IToDoItem>().ToList();
            currentUserId = userId;
        }

        private  void AddItemAsync(IToDoItem todo)
        {
            try
            {
                toDoManagerClient.CreateToDoItem(todo.ToRemoteToDoItem());               
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        private void DeleteItemAsync(IToDoItem todo)
        {
            try
            {
                ToDoItem realItemToDelete = GetRealItem(todo);
                if(realItemToDelete != null) 
                    toDoManagerClient.DeleteToDoItem(realItemToDelete.ToDoId);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private ToDoItem GetRealItem(IToDoItem todo)
        {
            List<ToDoItem> realItems = toDoManagerClient.GetTodoList(currentUserId).ToList();
            ToDoItem realItem =
                realItems.FirstOrDefault(x => x.IsCompleted == todo.IsCompleted && x.Name == todo.Name);
            return realItem;
        }

        private void UpdateItemAsync(IToDoItem todo)
        {
            try
            {
                ToDoItem realItemToUpdate = GetRealItem(todo);
                if (realItemToUpdate != null)
                    toDoManagerClient.UpdateToDoItem(realItemToUpdate);
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}

