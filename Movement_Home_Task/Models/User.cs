using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Movement_Home_Task.Models
{
    public enum Role { Admin, User}
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("Role")]
        public Role? Role { get; set; }
        [BsonElement("CreatedAt")]
        public DateTime CreatedAt { get; set; }
        [BsonElement("Description")]
        public string? Description { get; set; }
    }
}
