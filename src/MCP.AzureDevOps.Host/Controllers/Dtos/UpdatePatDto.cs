using System.ComponentModel.DataAnnotations;

namespace MCP.AzureDevOps.Host.Controllers.Dtos;

public sealed record UpdatePatDto([Required, MinLength(1)] string Pat);
