using System;

namespace Godfrey.Models.Common
{
    public interface IVersionedEntity
    {
        Guid Version { get; set; }
    }
}
