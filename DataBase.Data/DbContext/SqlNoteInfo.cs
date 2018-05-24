using System.Diagnostics;
using System.Reflection;
using UniversityDesign.Data;

namespace UniversityDesign.Data
{
    /// <summary>
    /// Sql注释
    /// </summary>
    public class SqlNoteInfo
    {
        private string _plat = string.Empty;
        private string _author = string.Empty;
        private string _description = string.Empty;
        private string _file = string.Empty;
        private StackFrame _stackFrame = null;
        private string _debugSql = string.Empty;
        private string _fun = string.Empty;

        public SqlNoteInfo(string plat, string author, string description, string file, string fun)
        {
            _plat = plat;
            _author = author;
            _description = description;
            _file = file;
            _fun = fun;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="plat">来源平台</param>
        /// <param name="author">作者</param>
        /// <param name="description">描述用途</param>
        /// <param name="file">文件</param>
        public SqlNoteInfo(string plat, string author, string description, string file)
            : this(plat, author, description, file, string.Empty)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="plat">来源平台</param>
        /// <param name="author">作者</param>
        /// <param name="description">描述用途</param>
        public SqlNoteInfo(string plat, string author, string description)
            : this(plat, author, description, string.Empty)
        {
        }

        public SqlNoteInfo(string author, string description)
        {
            _author = author;
            _description = description;
            _file = string.Empty;
        }

        /// <summary>
        /// 来源平台
        /// </summary>
        public string Plat
        {
            get { return _plat; }
            set { _plat = value; }
        }

        /// <summary>
        /// 作者
        /// </summary>
        public string Author
        {
            get { return _author; }
            set { _author = value; }
        }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Fun
        {
            get { return _fun; }
            set { _fun = value; }
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string File
        {
            get { return _file; }
            set { _file = value; }
        }

        public string DebugSQL
        {
            get { return _debugSql; }
            set { _debugSql = value; }
        }

        public StackFrame StackFrame
        {
            get { return _stackFrame; }
            set { _stackFrame = value; }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.File) && this.StackFrame != null)
            {
                MethodBase method = this.StackFrame.GetMethod();
                if (method != null && method.ReflectedType != null)
                    this.File = string.Format("{0}.{1}", method.ReflectedType.Name ?? string.Empty, method.Name ?? string.Empty);
            }
            string str = "  /*Flat:{0}/Author:{1}/For:{2}/File:{3}/Fun:{4}*/  ";
            return string.Format(str, this.Plat ?? string.Empty, this.Author ?? string.Empty, this.Description ?? string.Empty, this.File, this.Fun ?? string.Empty);
        }
    }
}