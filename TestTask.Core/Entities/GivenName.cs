﻿using TestTask.Core.Interfaces;

namespace TestTask.Core.Entities;

public class GivenName : IIdentifiablyEntity<Guid> 
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public Guid HumanNameId { get; set; }
}
