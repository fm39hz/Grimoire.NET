namespace Grimoire.Application.Dto.Common;

using System;

public interface ITimestampedDto {
	public DateTime? CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}
