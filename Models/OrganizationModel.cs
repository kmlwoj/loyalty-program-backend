using System.Text.Json.Serialization;

namespace lojalBackend.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrgTypes
    {
        Client,
        Shop,
        Swagger
    }
    public class OrganizationModel
    {
        public string Name { get; set; }
        public OrgTypes Type { get; set; }
        public OrganizationModel()
        {
            Name = string.Empty;
            Type = OrgTypes.Client;
        }
        public OrganizationModel(string name, OrgTypes type)
        {
            Name = name;
            Type = type;
        }
    }
}
