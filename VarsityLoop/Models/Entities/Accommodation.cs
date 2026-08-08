using Google.Cloud.Firestore;
using VarsityLoop.Models.Common;

namespace VarsityLoop.Models.Entities
{
    public enum ResidenceClassification
    {
        Accredited,
        Private
    }

    public enum AccommodationType
    {
        SingleRoom,
        SharingRoom,
        Bachelor,
        Studio,
        OneBedroom,
        TwoBedroom,
        ThreeBedroom,
        FourBedroomPlus
    }

    public enum GenderPreference
    {
        Any,
        Male,
        Female
    }

    public enum AccommodationStatus
    {
        Active,
        Paused,

        // Moderator-only states (Phase 14 - Accommodation CMS).
        Suspended,
        Removed
    }

    /// <summary>
    /// Stored in the "Accommodations" collection. Deliberately NOT part of the
    /// Marketplace Listing system (no Category/Module) - Accommodation is its
    /// own platform area with its own field shape, its own browsing UI, and
    /// (from Phase 12 onward) its own landlord verification gate on who may
    /// publish a residence at all.
    /// </summary>
    [FirestoreData]
    public class Accommodation : BaseEntity
    {
        [FirestoreProperty("residenceName")]
        public string ResidenceName { get; set; } = string.Empty;

        [FirestoreProperty("classification")]
        public string Classification { get; set; } = ResidenceClassification.Private.ToString();

        [FirestoreProperty("accommodationType")]
        public string AccommodationType { get; set; } = Entities.AccommodationType.SingleRoom.ToString();

        [FirestoreProperty("monthlyRent")]
        public double MonthlyRent { get; set; }

        [FirestoreProperty("deposit")]
        public double Deposit { get; set; }

        [FirestoreProperty("university")]
        public string University { get; set; } = string.Empty;

        [FirestoreProperty("distanceFromCampus")]
        public string? DistanceFromCampus { get; set; }

        [FirestoreProperty("availableFrom")]
        public Timestamp AvailableFrom { get; set; } = Timestamp.GetCurrentTimestamp();

        [FirestoreProperty("leasePeriod")]
        public string? LeasePeriod { get; set; }

        [FirestoreProperty("genderPreference")]
        public string GenderPreference { get; set; } = Entities.GenderPreference.Any.ToString();

        [FirestoreProperty("description")]
        public string Description { get; set; } = string.Empty;

        [FirestoreProperty("googleMapsUrl")]
        public string? GoogleMapsUrl { get; set; }

        [FirestoreProperty("gallery")]
        public List<string> Gallery { get; set; } = new();

        [FirestoreProperty("landlordId")]
        public string LandlordId { get; set; } = string.Empty;

        [FirestoreProperty("landlordName")]
        public string LandlordName { get; set; } = string.Empty;

        [FirestoreProperty("status")]
        public string Status { get; set; } = AccommodationStatus.Active.ToString();

        [FirestoreProperty("views")]
        public int Views { get; set; } = 0;
    }
}
