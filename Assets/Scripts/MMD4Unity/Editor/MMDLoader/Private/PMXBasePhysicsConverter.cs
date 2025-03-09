using UnityEngine;
using MMD.PMX;

namespace MMD
{
    public abstract class PMXBasePhysicsConverter
    {
        protected GameObject            root_game_object_;
        protected GameObject[]          bone_game_objs;
        protected PMXFormat             format_;
        protected float                 scale_;
        
        public PMXBasePhysicsConverter(GameObject root_game_object, PMXFormat format, GameObject[] bones_objs, float scale)
        {
            root_game_object_ = root_game_object;
            format_ = format;
            bone_game_objs = bones_objs;
            scale_ = scale;
        }

        public abstract void Convert();

        /// <summary>
        /// ファイルパス文字列の取得
        /// </summary>
        /// <returns>ファイルパスに使用可能な文字列</returns>
        /// <param name='src'>ファイルパスに使用したい文字列</param>
        protected static string GetFilePathString(string src) {
            return src.Replace('\\', '＼')
                        .Replace('/',  '／')
                        .Replace(':',  '：')
                        .Replace('*',  '＊')
                        .Replace('?',  '？')
                        .Replace('"',  '”')
                        .Replace('<',  '＜')
                        .Replace('>',  '＞')
                        .Replace('|',  '｜')
                        .Replace("\n",  string.Empty)
                        .Replace("\r",  string.Empty);
        }
    }
}