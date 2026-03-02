using Newtonsoft.Json;

namespace Aco228.FacebookAdLibrary.Models.Extract;

public class LibraryAdModel
{
    public string id => node.collated_results.FirstOrDefault()?.ad_archive_id ?? "";
    public NodeDTO node { get; set; }
    public object cursor { get; set; }
}


public class BodyDTO
{
    public string text { get; set; }
}

public class ImageDTO
{
    public List<object> image_crops { get; set; }
    public string original_image_url { get; set; }
    public string resized_image_url { get; set; }
    public string watermarked_resized_image_url { get; set; }
}

public class VideoDTO
{
    public string video_preview_image_url { get; set; }
    public string video_sd_url { get; set; }
}

public class CardDto
{
    public string body { get; set; }
    public string cta_type { get; set; }
    public string caption { get; set; }
    public string link_description { get; set; }
    public string link_url { get; set; }
    public string title { get; set; }
    public string cta_text { get; set; }
    public string video_hd_url { get; set; }
    public string video_preview_image_url { get; set; }
    public string video_sd_url { get; set; }
    public object watermarked_video_hd_url { get; set; }
    public object watermarked_video_sd_url { get; set; }
    public List<object> image_crops { get; set; }
    public string original_image_url { get; set; }
    public string resized_image_url { get; set; }
    public string watermarked_resized_image_url { get; set; }
}

public class SnapshotDTO
{
    public object branded_content { get; set; }
    public string page_id { get; set; }
    public bool page_is_deleted { get; set; }
    public string page_profile_uri { get; set; }
    public object root_reshared_post { get; set; }
    public string byline { get; set; }
    public object disclaimer_label { get; set; }
    public string page_name { get; set; }
    public string page_profile_picture_url { get; set; }
    [JsonProperty("event")]
    public object Event { get; set; }
    public string caption { get; set; }
    public string cta_text { get; set; }
    public List<CardDto>? cards { get; set; }
    public BodyDTO? body { get; set; }
    public string cta_type { get; set; }
    public string display_format { get; set; }
    public string link_description { get; set; }
    public string link_url { get; set; }
    public List<ImageDTO>? images { get; set; }
    public List<string> page_categories { get; set; }
    public int page_like_count { get; set; }
    public string title { get; set; }
    public List<VideoDTO>? videos { get; set; }
    public bool is_reshared { get; set; }
    public List<object> extra_links { get; set; }
    public List<object> extra_texts { get; set; }
    public List<object> extra_images { get; set; }
    public List<object> extra_videos { get; set; }
    public object country_iso_code { get; set; }
    public object brazil_tax_id { get; set; }
    public object additional_info { get; set; }
    public List<object> ec_certificates { get; set; }
}

public class ImpressionsWithIndexDTO
{
    public object impressions_text { get; set; }
    public int impressions_index { get; set; }
}

public class FinservDTO
{
    public bool is_deemed_finserv { get; set; }
    public bool is_limited_delivery { get; set; }
}

public class TwAntiScamDTO
{
    public bool is_limited_delivery { get; set; }
}

public class RegionalRegulationDataDTO
{
    public FinservDTO finserv { get; set; }
    public TwAntiScamDTO tw_anti_scam { get; set; }
}

public class CollatedResultDTO
{
    public string? ad_archive_id { get; set; }
    public int? collation_count { get; set; }
    public string? collation_id { get; set; }
    public string? page_id { get; set; }
    public SnapshotDTO? snapshot { get; set; }
    public bool? is_active { get; set; }
    public bool? has_user_reported { get; set; }
    public object? report_count { get; set; }
    public List<object>? menu_items { get; set; }
    public object? state_media_run_label { get; set; }
    public bool? page_is_deleted { get; set; }
    public string? page_name { get; set; }
    public ImpressionsWithIndexDTO? impressions_with_index { get; set; }
    public string? gated_type { get; set; }
    public List<string>? categories { get; set; }
    public bool? is_aaa_eligible { get; set; }
    public bool? contains_digital_created_media { get; set; }
    public object? reach_estimate { get; set; }
    public string? currency { get; set; }
    public object? spend { get; set; }
    public long? end_date { get; set; }
    public List<string>? publisher_platform { get; set; }
    public long? start_date { get; set; }
    public bool? contains_sensitive_content { get; set; }
    public object? total_active_time { get; set; }
    public RegionalRegulationDataDTO? regional_regulation_data { get; set; }
    public string? hide_data_status { get; set; }
    public object? fev_info { get; set; }
    public object? ad_id { get; set; }
    public List<object>? targeted_or_reached_countries { get; set; }
}

public class NodeDTO
{
    public List<CollatedResultDTO> collated_results { get; set; }
    public string __typename { get; set; }
}
