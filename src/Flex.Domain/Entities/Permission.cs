using Flex.Domain.Abstractions;

namespace Flex.Domain.Entities
{
    public class Permission : EntityBase<long>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long? ParentPermissionId { get; set; }
        public int IsAssignable { get; set; } = 1;
        public int SortOrder { get; set; }
        public int IsActive { get; set; }
        public int IsCrudRule { get; set; }

        #region Navigation Properties
        public virtual Permission? ParentPermission { get; set; }
        public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();
        #endregion
    }
}
