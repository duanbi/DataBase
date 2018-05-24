using System;
using DataBase.Data;
namespace DataBase.Test.TableEntity
{
    ///<summary>
    ///Bank
    ///</summary>
    [Table("Bank")]
    public class BankEntity:Entity<int>
    {
        #region 默认值
        public BankEntity()
        {
            Name = string.Empty;
            CreateDate = new DateTime(1900,01,01);
            Remark = string.Empty;
            Code = string.Empty;
            IsUseUnitedCredit = true;
            IsShowOnApp = true;
            IsShow = true;
        }
        #endregion

        ///<summary>
        ///Id
        ///</summary>
        [Key]
        [Column("Id",0)]
        public int Id { get; set; }
        ///<summary>
        ///Name
        ///</summary>
        [Column("Name",50)]
        public virtual string Name{ get; set; }
        ///<summary>
        ///CreateDate
        ///</summary>
        [Column("CreateDate",0)]
        public virtual DateTime CreateDate{ get; set; }
        ///<summary>
        ///Remark
        ///</summary>
        [Column("Remark",500)]
        public virtual string Remark{ get; set; }
        ///<summary>
        ///Code
        ///</summary>
        [Column("Code",-1)]
        public virtual string Code{ get; set; }
        ///<summary>
        ///IsUseUnitedCredit
        ///</summary>
        [Column("IsUseUnitedCredit",0)]
        public virtual bool IsUseUnitedCredit{ get; set; }
        ///<summary>
        ///IsShowOnApp
        ///</summary>
        [Column("IsShowOnApp",0)]
        public virtual bool IsShowOnApp{ get; set; }
        ///<summary>
        ///IsShow
        ///</summary>
        [Column("IsShow",0)]
        public virtual bool IsShow{ get; set; }
    }
}

