using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using KirylLencykToDoManager.ToDoManagerServiceReference;
using ToDo.Infrastructure;

namespace KirylLencykToDoManager
{
    public class ToDoManager : ToDo.Infrastructure.IToDoManager
    {
        private int currentUserId;

        private readonly ToDoManagerServiceReference.ToDoManagerClient toDoManagerClient;

        private static readonly string queueXml = "queue.xml";

        private List<IToDoItem> localItems;

        private ConcurrentDictionary<ToDoItemWorkItem, int> workQueue;

        private readonly object sync;

        public ToDoManager()
        {
            toDoManagerClient = new ToDoManagerServiceReference.ToDoManagerClient();
            workQueue = new ConcurrentDictionary<ToDoItemWorkItem, int>();
            sync = new object();
            LoadWorkQueue();
            InvokeWorkQueue();
        }


        public List<IToDoItem> GetTodoList(int userId)
        {
            if (currentUserId != userId) LoadLocalStorage(userId);

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
                Task.Run(() => UpdateItemRemote(todo));
                Task.Run(() => InvokeWorkQueue());
            }
        }

        public void CreateToDoItem(IToDoItem todo)
        {
            if (todo.UserId != currentUserId) LoadLocalStorage(todo.UserId);

            todo.ToDoId = localItems.Count;
            localItems.Add(todo);
            Task.Run(() => AddItemRemote(todo));
            Task.Run(() => InvokeWorkQueue());
        }



        public void DeleteToDoItem(int todoItemId)
        {
            IToDoItem toDelete = localItems.Find(x => x.ToDoId == todoItemId);
            if (toDelete != null)
            {
                localItems.Remove(toDelete);
                Task.Run(() => DeleteItemRemote(toDelete));
                Task.Run(() => InvokeWorkQueue());
            }
        }

        public int CreateUser(string name)
        {
            InvokeWorkQueue();
            currentUserId = toDoManagerClient.CreateUser(name);
            LoadLocalStorage(currentUserId);
            return currentUserId;
        }

        private void LoadLocalStorage(int userId)
        {
            localItems = toDoManagerClient.GetTodoList(userId).Cast<IToDoItem>().ToList();
            currentUserId = userId;
        }

        private void AddItemRemote(IToDoItem todo)
        {
            var currentWorkItem = new ToDoItemWorkItem {Item = todo.ToRemoteToDoItem(), WorkType = ToDoWorkType.Add};
            workQueue.TryAdd(currentWorkItem,1);
            SaveWorkQueue();
            try
            {
                toDoManagerClient.CreateToDoItem(todo.ToRemoteToDoItem());
                DeleteFromWorkQueue(currentWorkItem);
            }
            catch (Exception)
            {
                ;
            }
        }


        private void DeleteItemRemote(IToDoItem todo)
        {
            var currentWorkItem = new ToDoItemWorkItem { Item = todo.ToRemoteToDoItem(), WorkType = ToDoWorkType.Add };
            workQueue.TryAdd(currentWorkItem, 1);
            SaveWorkQueue();
            try
            {
                ToDoItem realItemToDelete = GetRealItem(todo);
                if (realItemToDelete != null)
                    toDoManagerClient.DeleteToDoItem(realItemToDelete.ToDoId);

                DeleteFromWorkQueue(currentWorkItem);
            }
            catch (Exception)
            {
                ;
            }
        }


       private void UpdateItemRemote(IToDoItem todo)
        {
            var currentWorkItem = new ToDoItemWorkItem { Item = todo.ToRemoteToDoItem(), WorkType = ToDoWorkType.Add };
            workQueue.TryAdd(currentWorkItem, 1);
            SaveWorkQueue();
            try
            {
                ToDoItem realItemToUpdate = GetRealItem(todo);
                if (realItemToUpdate != null)
                    toDoManagerClient.UpdateToDoItem(realItemToUpdate);
                DeleteFromWorkQueue(currentWorkItem);
            }
            catch (Exception)
            {
                ;
            }
        }

        private void DeleteFromWorkQueue(ToDoItemWorkItem currentWorkItem)
        {
            int i;
            workQueue.TryRemove(currentWorkItem, out i);
            SaveWorkQueue();
        }

        private ToDoItem GetRealItem(IToDoItem todo)
        {
            List<ToDoItem> realItems = toDoManagerClient.GetTodoList(currentUserId).ToList();
            ToDoItem realItem =
                realItems.FirstOrDefault(x => x.IsCompleted == todo.IsCompleted && x.Name == todo.Name);
            return realItem;
        }

        private void InvokeToDoWorkItem(ToDoItemWorkItem item)
        {
            switch (item.WorkType)
            {
                case ToDoWorkType.Add:
                    AddItemRemote(item.Item);
                    break;
                case ToDoWorkType.Remove:
                    DeleteItemRemote(item.Item);
                    break;
                case ToDoWorkType.Update:
                    UpdateItemRemote(item.Item);
                    break;
            }
        }

        private void SaveWorkQueue()
        {
            lock (sync)
            {

                if (workQueue.Count != 0)
                {
                    using (var fs = new FileStream(queueXml, FileMode.Create))
                    {
                        var serializer = new XmlSerializer(typeof(List<ToDoItemWorkItem>));
                        serializer.Serialize(fs, workQueue.Keys.ToList());
                    }
                }
                else
                {
                    if (File.Exists(queueXml))
                    {
                        File.Delete(queueXml);
                    }
                }
            }
        }

        private void InvokeWorkQueue()
        {
            ConcurrentDictionary<ToDoItemWorkItem, int> localQueueCopy;
            lock (sync)
            {
                localQueueCopy = workQueue;
                workQueue = new ConcurrentDictionary<ToDoItemWorkItem, int>();
            }
            foreach (ToDoItemWorkItem workItem in localQueueCopy.Keys)
            {
                InvokeToDoWorkItem(workItem);
            }
            if (workQueue.IsEmpty)
            {
                File.Delete(queueXml);
            }
        }

        private void LoadWorkQueue()
        {
            lock (sync)
            {

                if (File.Exists(queueXml))
                {
                    using (var fs = new FileStream(queueXml, FileMode.Open))
                    {
                        var serializer = new XmlSerializer(typeof(List<ToDoItemWorkItem>));
                        var list = (List<ToDoItemWorkItem>)serializer.Deserialize(fs);
                        workQueue =
                            new ConcurrentDictionary<ToDoItemWorkItem, int>();
                        foreach (ToDoItemWorkItem item in list)
                        {
                            workQueue.TryAdd(item, 1);
                        }
                    }
                    File.Delete(queueXml);
                }
            }
        }
    }

}

