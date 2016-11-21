using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoListService.Models;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TodoListController : Controller
    {
        TodoItemContainer m_container;

        public TodoListController(TodoItemContainer container)
        {
            m_container = container;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<TodoItem> Get()
        {
            string owner = (User.FindFirst(ClaimTypes.NameIdentifier))?.Value;
            return m_container.TodoStore.Where(t => t.Owner == owner).ToList();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]TodoItem Todo)
        {
            string owner = (User.FindFirst(ClaimTypes.NameIdentifier))?.Value;
            m_container.TodoStore.Add(new TodoItem { Owner = owner, Title = Todo.Title });
        }
    }
}
