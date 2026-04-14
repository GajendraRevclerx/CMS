using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Collections.Generic;

namespace CMS.Models
{
    public class BsonStringToListSerializer : SerializerBase<List<string>>
    {
        public override List<string> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.CurrentBsonType;

            if (bsonType == BsonType.String)
            {
                var value = context.Reader.ReadString();
                return new List<string> { value };
            }
            else if (bsonType == BsonType.Array)
            {
                return BsonSerializer.Deserialize<List<string>>(context.Reader);
            }
            else if (bsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return new List<string>();
            }
            else
            {
                context.Reader.SkipValue();
                return new List<string>();
            }
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, List<string> value)
        {
            if (value == null)
            {
                context.Writer.WriteNull();
            }
            else
            {
                BsonSerializer.Serialize(context.Writer, value);
            }
        }
    }
}
