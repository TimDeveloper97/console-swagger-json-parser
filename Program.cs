using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
public class SchemaObjectConverter : JsonConverter<SchemaObject>
{
    public override SchemaObject ReadJson(JsonReader reader, Type objectType, SchemaObject existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var schemaObject = new SchemaObject();

        if (reader.TokenType == JsonToken.StartObject)
        {
            JObject jObject = JObject.Load(reader);
            schemaObject.Type = jObject.GetValue("type")?.Value<string>();
            schemaObject.Format = jObject.GetValue("format")?.Value<string>();

            if (jObject.TryGetValue("$ref", out JToken refToken))
            {
                schemaObject.Ref = refToken.ToObject<string>();
            }

            if (jObject.TryGetValue("xml", out JToken xmlToken))
            {
                schemaObject.Xml = xmlToken.ToObject<object>();
            }

            if (jObject.TryGetValue("properties", out JToken propertiesToken))
            {
                var dicPros = propertiesToken.ToObject<Dictionary<string, JToken>>();
                if (dicPros != null) schemaObject.Properties = new List<Property>();

                foreach (var pros in dicPros)
                {
                    var pro = new Property();
                    
                    // get field
                    var convert = pros.Value.ToObject<Property>();
                    if(convert != null)
                        pro = convert;

                    pro.Ref = pros.Value["$ref"]?.ToString();
                    pro.Name = pros.Key;

                    //items
                    var itemsToken = pros.Value["items"];
                    if (itemsToken != null)
                    {
                        //pro.Name = itemsToken["name"]?.ToObject<string>();
                        pro.Ref = itemsToken["$ref"]?.ToObject<string>();
                        if(itemsToken["type"] != null) pro.Type += "/" + itemsToken["type"]?.ToObject<string>();
                    }

                    schemaObject.Properties.Add(pro);
                }
            }

            if (jObject.TryGetValue("required", out JToken requiredToken))
            {
                schemaObject.Required = requiredToken.ToObject<List<string>>();
            }


            if (jObject.TryGetValue("items", out JToken itemsToken1))
            {
                if(itemsToken1 != null)
                {
                    schemaObject.NameParameter = itemsToken1["name"]?.ToObject<string>();
                    schemaObject.Ref = itemsToken1["$ref"]?.ToObject<string>();
                    schemaObject.Type = String.Join("/", itemsToken1["type"]?.ToObject<string>());
                }    
            }
        }

        return schemaObject;
    }

    public override void WriteJson(JsonWriter writer, SchemaObject value, JsonSerializer serializer)
    {
        writer.WriteStartObject();

        if (!string.IsNullOrEmpty(value.Ref))
        {
            writer.WritePropertyName("$ref");
            writer.WriteValue(value.Ref);
        }
        else
        {
            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);

            if (!string.IsNullOrEmpty(value.Format))
            {
                writer.WritePropertyName("format");
                writer.WriteValue(value.Format);
            }
        }

        writer.WriteEndObject();
    }
}

public class SwaggerV2
{
    public string Swagger { get; set; }
    public object Info { get; set; }
    public string Host { get; set; }
    public string BasePath { get; set; }

    private object paths;
    public object Paths
    {
        get => paths; set
        {
            var lpath = new List<Path>();
            var dicpath = JsonConvert.DeserializeObject<Dictionary<string, object>>(value.ToString());

            foreach (var path in dicpath)
            {
                var p = new Path();

                var dicmethod = JsonConvert.DeserializeObject<Dictionary<string, object>>(path.Value.ToString());
                foreach (var method in dicmethod)
                {
                    p = JsonConvert.DeserializeObject<Path>(method.Value.ToString());

                    p.Method = method.Key;
                    p.Url = path.Key;

                    lpath.Add(p);
                }
            }

            paths = lpath;
        }

    }
    private object definitions;
    public object Definitions
    {
        get => definitions; set
        {
            var lschema = new List<SchemaObject>();
            var dicschema = JsonConvert.DeserializeObject<Dictionary<string, object>>(value.ToString());

            foreach (var schema in dicschema)
            {
                var p = new SchemaObject();
                p = JsonConvert.DeserializeObject<SchemaObject>(schema.Value.ToString());
                
                //get name
                if(p.Xml != null)
                {
                    var xmlObject = JObject.Parse(p.Xml.ToString());
                    p.Name = xmlObject["name"].ToString();
                }

                p.NameParameter = schema.Key;
                lschema.Add(p);
            }

            definitions = lschema;
        }
    }
}

public class Path
{
    public string Url { get; set; }
    public string Method { get; set; }
    public string OperationId { get; set; }
    public List<string> Tags { get; set; }
    public List<string> consumes { get; set; }
    public List<string> produces { get; set; }
    public List<Parameter> Parameters { get; set; }
}

public class Parameter
{
    public string Name { get; set; }
    public string In { get; set; }
    public string Description { get; set; }
    public SchemaObject Schema { get; set; }
}

public class SchemaObject
{
    public string NameParameter { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    [JsonProperty("$ref")]
    public string Ref { get; set; }
    public string Format { get; set; }
    public object Xml { get; set; }

    public List<Property> Properties { get; set; }
    public List<string> Required { get; set; }
}
public class Property
{
    public string Type { get; set; }
    [JsonProperty("$ref")]
    public string Ref { get; set; }
    public string Name { get; set; }
    public string Format { get; set; }
    public string Description { get; set; }
    public List<string> Enum { get; set; }
    public bool Required { get; set; }
    public object Default { get; set; }
    public object Example { get; set; }
    public int? Minimum { get; set; }
    public int? Maximum { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string Pattern { get; set; }
    public object Items { get; set; }
    public List<Property> Properties { get; set; }
    public bool ReadOnly { get; set; }
    public bool WriteOnly { get; set; }
    public bool Deprecated { get; set; }
}
class Program
{
    static async Task Main()
    {
        string json = @"{
            ""Swagger"": ""2.0"",
            ""Info"": {},
            ""Host"": ""example.com"",
            ""BasePath"": """",
            ""Paths"": {
                ""/path1"": {
                    ""get"": {
                        ""Url"": ""/path1"",
                        ""Method"": ""get"",
                        ""OperationId"": ""123"",
                        ""Tags"": [],
                        ""consumes"": [],
                        ""produces"": [],
                        ""Parameters"": [
                            {
                                ""Name"": ""param1"",
                                ""In"": ""query"",
                                ""Description"": ""Sample parameter"",
                                ""Schema"": {
                                    ""Type"": ""string"",
                                    ""Format"": ""email"",
                                    ""$ref"": ""#/definitions/Category"",
                                    ""properties"": {
                                            ""id"": {
                                              ""type"": ""integer"",
                                              ""format"": ""int64""
                                            },
                                            ""name"": {
                                              ""type"": ""string""
                                            },
                                        },
                                    ""required"": [
                                            ""name"",
                                            ""photoUrls""
                                          ],
                                    ""xml"": {
                                        ""name"": ""Tag""        
                                    }
                                }
                            }
                        ]
                    }
                }
            }
        }";
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new SchemaObjectConverter() },
            Formatting = Formatting.Indented
        };
        string swaggerUiUrl = "https://petstore.swagger.io/v2/swagger.json";
        string htmlContent = null;
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(swaggerUiUrl);
                response.EnsureSuccessStatusCode();

                htmlContent = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        var swagger = JsonConvert.DeserializeObject<SwaggerV2>(htmlContent);
        Path path = (swagger.Paths as List<Path>)[1];
        var outJson = JsonConvert.SerializeObject(swagger);
        Console.WriteLine(outJson);

        Console.WriteLine("Url: " + path.Url);
        Console.WriteLine("Method: " + path.Method);
        Console.WriteLine("OperationId: " + path.OperationId);
        Console.WriteLine("Tags: " + string.Join(", ", path.Tags ?? new List<string>()));
        Console.WriteLine("Consumes: " + string.Join(", ", path.consumes ?? new List<string>()));
        Console.WriteLine("Produces: " + string.Join(", ", path.produces ?? new List<string>()));
        Console.WriteLine("Parameters:");

        foreach (Parameter parameter in path.Parameters)
        {
            Console.WriteLine("  Name: " + parameter.Name);
            Console.WriteLine("  In: " + parameter.In);
            Console.WriteLine("  Description: " + parameter.Description);
            Console.WriteLine("  Schema Type: " + parameter.Schema?.Type);
            Console.WriteLine("  Schema Format: " + parameter.Schema?.Format);
            Console.WriteLine("  Schema Ref: " + parameter.Schema?.Ref);
            Console.WriteLine("  Schema Xml: " + parameter.Schema?.Xml);
            Console.WriteLine("  Schema Properties: " + parameter.Schema?.Properties);
            Console.WriteLine("  Schema Required: " + string.Join(", ", parameter.Schema?.Required ?? new List<string>()));
            
        }

        var definitions = swagger.Definitions as List<SchemaObject>;
        foreach (SchemaObject definition in definitions)
        {
            Console.WriteLine("-=---------------------------------");
            Console.WriteLine("  schemaObject Name: " + definition.Name);
            Console.WriteLine("  schemaObject Type: " + definition.Type);
            Console.WriteLine("  schemaObject NameType: " + definition.NameParameter);
            Console.WriteLine("  schemaObject Format: " + definition.Format);
            Console.WriteLine("  schemaObject Xml: " + definition.Xml);
            Console.WriteLine("  schemaObject Required: " + string.Join(", ", definition.Required ?? new List<string>()));
            for (int i = 0; i < definition.Properties.Count; i++)
            {
                definition.Properties[i] = Loop(definition.Properties[i], definitions);
            }
            
            Console.WriteLine("  schemaObject Ref: " + definition.Ref);
            Console.WriteLine("-=---------------------------------");
        }
    }

    static Property Loop(Property property, List<SchemaObject> definitions)
    {
        var @ref1 = property.Ref;
        if (@ref1 != null)
        {
            property.Properties = new List<Property>();
            var nameModel = @ref1.Split('/');
            var model = definitions.FirstOrDefault(x => x.Name == nameModel.LastOrDefault());

            property.Properties.AddRange(model.Properties);

            foreach (var item in property.Properties)
                Loop(item, definitions);
        }

        return property;
    }
}