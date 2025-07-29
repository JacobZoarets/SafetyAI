using System.Collections.Generic;

namespace SafetyAI.Models.DTOs
{
    public class UserContext
    {
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public string Location { get; set; }
        public List<string> Permissions { get; set; }
        public string Department { get; set; }
        public string Language { get; set; } = "en";

        public UserContext()
        {
            Permissions = new List<string>();
        }
    }
}