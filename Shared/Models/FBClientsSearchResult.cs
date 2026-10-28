#nullable enable

using System;

namespace GIBS.Module.FBClients.Models
{
    public class FBClientsSearchRequest
    {
        public int ModuleId { get; set; }
        public string? ClientId { get; set; }
        public string? ClientIdCard { get; set; }
        public string? ClientFirstName { get; set; }
        public string? ClientLastName { get; set; }
        public DateOnly? ClientDOB { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientCity { get; set; }
        public string? ClientType { get; set; }
        public bool IncludeInactive { get; set; }

        public bool IsAfmSearch { get; set; }
        public string? AfmFirstName { get; set; }
        public string? AfmLastName { get; set; }
        public DateOnly? AfmDOB { get; set; }
    }

    public class FBClientsSearchResult
    {
        public int FBClientsId { get; set; }
        public int? LegacyClientId { get; set; }
        public string? ClientFirstName { get; set; }
        public string? ClientMiddleInitial { get; set; }
        public string? ClientLastName { get; set; }
        public string? ClientSuffix { get; set; }
        public DateOnly? ClientDOB { get; set; }
        public bool ClientDOBVerify { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientUnit { get; set; }
        public string? ClientCity { get; set; }
        public string? ClientTown { get; set; }
        public string? ClientState { get; set; }
        public string? ClientZipCode { get; set; }
        public string? ClientType { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsLocked { get; set; }
        public string? ClAddFamMemFirstName { get; set; }
        public string? ClAddFamMemLastName { get; set; }
        public DateOnly? LastVisitDate { get; set; }
        public int? VisitNumBags { get; set; }
    }
}
