using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Inventory;

namespace MinecraftClient.Resource
{
    public class ItemModelPredicate
    {
        public static readonly ItemModelPredicate EMPTY = new ItemModelPredicate(new());

        private Dictionary<string, float> conditions = new Dictionary<string, float>();

        public ItemModelPredicate(Dictionary<string, float> conditions)
        {
            this.conditions = conditions;
        }

        public static ItemModelPredicate fromJson(Json.JSONData data)
        {
            if (data.Properties.Count == 0)
                return EMPTY;
            
            var conditions = new Dictionary<string, float>();
            foreach (var src in data.Properties)
            {
                float value;

                if (float.TryParse(src.Value.StringValue, out value))
                    conditions.Add(src.Key, value);
                else
                    Debug.LogWarning($"Invalid item model predicate value for key {{src.Key}}: {src.Value.StringValue}");
            }
            return new(conditions);
        }

        public bool check(ItemStack itemStack)
        {
            // TODO Implement

            return true;

        }

        public override string ToString()
        {
            if (conditions.Count > 0)
            {
                var con = conditions.GetEnumerator();
                con.MoveNext();
                string content = $"{con.Current.Key}={con.Current.Value}";

                while (con.MoveNext())
                    content += $",{con.Current.Key}={con.Current.Value}";

                return $"{{{content}}}"; // Escape braces in string interpolation by doubling them
            }
            else return "{}";
        }

    }

}
