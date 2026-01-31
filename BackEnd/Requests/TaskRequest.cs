using System.ComponentModel.DataAnnotations;

namespace Backend.Requests
{
    public class TaskRequest
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public bool IsCompleted { get; set; }
        
        public DateTime Ts { get; set; }
    }
}
