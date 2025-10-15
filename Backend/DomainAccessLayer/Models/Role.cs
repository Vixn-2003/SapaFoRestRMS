using System;
using System.Collections.Generic;

namespace DomainAccessLayer.Models;

public partial class Role
{
    /*
     1 - 0wner
     2 - Admin
     3 - Manager
     4 - Staff
     5 - Customer
   
     */
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
