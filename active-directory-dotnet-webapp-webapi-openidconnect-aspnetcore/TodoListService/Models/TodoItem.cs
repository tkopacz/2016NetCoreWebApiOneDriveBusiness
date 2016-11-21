using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListService.Models
{
    public class TodoItemContainer
    {
        public ConcurrentBag<TodoItem> TodoStore = new ConcurrentBag<TodoItem>();
    }



    public class TodoItem
    {
        public string Owner { get; set; }
        public string Title { get; set; }
    }
}
