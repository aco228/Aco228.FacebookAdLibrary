namespace Aco228.FacebookAdLibrary.Models.Extract;


public class AdDetailsDTO
{
    public AaaInfoDTO aaa_info { get; set; }
    public List<object> violation_types { get; set; }
    public object verified_voice_context { get; set; }
    public TransparencyByLocationDTO transparency_by_location { get; set; }
    public bool is_siep_advertiser_eligible_for_ai_disclosure { get; set; }
    public bool is_violating_eu_siep { get; set; }
}


public class PageSpendDTO
{
    public object current_week { get; set; }
    public List<object> lifetime_by_disclaimer { get; set; }
    public List<object> weekly_by_disclaimer { get; set; }
    public bool is_political_page { get; set; }
}

public class PageInfoDTO
{
    public string entity_type { get; set; }
    public object ig_followers { get; set; }
    public object ig_username { get; set; }
    public object ig_verification { get; set; }
    public int likes { get; set; }
    public string page_alias { get; set; }
    public string page_category { get; set; }
    public string page_cover_photo { get; set; }
    public string page_id { get; set; }
    public bool page_is_deleted { get; set; }
    public bool page_is_restricted { get; set; }
    public string page_name { get; set; }
    public string page_profile_uri { get; set; }
    public string page_verification { get; set; }
    public string profile_photo { get; set; }
    public bool is_profile_page { get; set; }
}

public class AdLibraryPageInfoDTO
{
    public PageSpendDTO page_spend { get; set; }
    public PageInfoDTO page_info { get; set; }
}


public class PayerBeneficiaryDataDTO
{
    public string payer { get; set; }
    public string beneficiary { get; set; }
}

public class AaaInfoDTO
{
    public List<PayerBeneficiaryDataDTO> payer_beneficiary_data { get; set; }
    public bool targets_eu { get; set; }
    public bool has_violating_payer_beneficiary { get; set; }
    public bool is_ad_taken_down { get; set; }
}

public class LocationAudienceDTO
{
    public string name { get; set; }
    public int num_obfuscated { get; set; }
    public string type { get; set; }
    public bool excluded { get; set; }
}

public class AgeAudienceDTO
{
    public int min { get; set; }
    public int max { get; set; }
}


public class AgeCountryGenderReachBreakdownDTO
{
    public string country { get; set; }
}

public class EuTransparencyDTO
{
    public bool targets_eu { get; set; }
    public List<LocationAudienceDTO> location_audience { get; set; }
    public string gender_audience { get; set; }
    public AgeAudienceDTO age_audience { get; set; }
    public int eu_total_reach { get; set; }
    public List<AgeCountryGenderReachBreakdownDTO> age_country_gender_reach_breakdown { get; set; }
}

public class TransparencyByLocationDTO
{
    public object br_transparency { get; set; }
    public EuTransparencyDTO eu_transparency { get; set; }
    public object uk_transparency { get; set; }
}


public class AdLibraryMainDTO
{
    public AdDetailsDTO ad_details { get; set; }
}

public class DataDTO
{
    public AdLibraryMainDTO ad_library_main { get; set; }
}

public class ServerMetadataDTO
{
    public long request_start_time_ms { get; set; }
    public long time_at_flush_ms { get; set; }
}

public class ExtensionsDTO
{
    public ServerMetadataDTO server_metadata { get; set; }
    public bool is_final { get; set; }
}
