using System;
using System.Text.RegularExpressions;

namespace AnimusForge;

// Only confirmed system material kinds. Never invoked by capture, save or alias owners.
internal static class CampaignSystemFactProjection
{
    private sealed class Template
    {
        internal readonly string Key; internal readonly string[] Parts, Roles;
        internal Template(string key, string[] roles) { Key=key; Parts=Regex.Split(PromptComposerTexts.Raw(key + "_source"), @"\{[0-9]+\}"); Roles=roles; }
    }
    private static readonly Regex Slots = new Regex(@"\{([0-9]+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex NoteBlocks = new Regex(PromptComposerTexts.Raw("campaign_fact_note_pattern"), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Template[] Civil = {
        new Template("campaign_fact_civil_recruit_crown", new string[] {"name"}),
        new Template("campaign_fact_civil_recruit_faction", new string[] {"name", "faction"}),
        new Template("campaign_fact_civil_join", new string[] {"name", "faction"}),
        new Template("campaign_fact_civil_leave", new string[] {"name"}),
        new Template("campaign_fact_civil_found_dissolved", new string[] {"name", "faction", "demand"}),
        new Template("campaign_fact_civil_found_left", new string[] {"name", "faction", "demand"}),
        new Template("campaign_fact_civil_found", new string[] {"name", "demand"}),
        new Template("campaign_fact_civil_suppressed", new string[] {"faction"}),
        new Template("campaign_fact_civil_conceded", new string[] {"faction", "notes"}),
        new Template("campaign_fact_civil_founded", new string[] {"faction", "demand"}),
        new Template("campaign_fact_civil_outbreak", new string[] {"faction", "name", "name"}),
        new Template("campaign_fact_civil_resolved", new string[] {"faction", "outcome", "notes"}),
        new Template("campaign_fact_civil_demand_adjusted", new string[] {"demand"}),
        new Template("campaign_fact_civil_fixed_89", new string[] {}),
        new Template("campaign_fact_civil_fixed_90", new string[] {}),
        new Template("campaign_fact_civil_fixed_91", new string[] {}),
        new Template("campaign_fact_civil_fixed_95", new string[] {}),
        new Template("campaign_fact_civil_fixed_96", new string[] {}),
        new Template("campaign_fact_civil_fixed_102", new string[] {}),
        new Template("campaign_fact_civil_fixed_103", new string[] {}),
        new Template("campaign_fact_civil_fixed_104", new string[] {}),
        new Template("campaign_fact_civil_fixed_105", new string[] {}),
        new Template("campaign_fact_civil_fixed_107", new string[] {}),
        new Template("campaign_fact_civil_fixed_108", new string[] {}),
        new Template("campaign_fact_civil_fixed_111", new string[] {}),
        new Template("campaign_fact_civil_fixed_112", new string[] {}),
        new Template("campaign_fact_civil_fixed_145", new string[] {}),
        new Template("campaign_fact_civil_fixed_146", new string[] {}),
        new Template("campaign_fact_civil_fixed_147", new string[] {}),
        new Template("campaign_fact_civil_fixed_148", new string[] {}),
        new Template("campaign_fact_civil_fixed_149", new string[] {}),
        new Template("campaign_fact_civil_fixed_150", new string[] {}),
        new Template("campaign_fact_civil_fixed_151", new string[] {}),
    };
    private static readonly Template[] Notes = {
        new Template("campaign_fact_note_rebel_dissolved", new string[] {"name"}),
        new Template("campaign_fact_note_pledge", new string[] {"name", "name"}),
        new Template("campaign_fact_note_peace", new string[] {"name", "name"}),
        new Template("campaign_fact_note_policy", new string[] {"name"}),
        new Template("campaign_fact_note_redress", new string[] {"name", "name"}),
        new Template("campaign_fact_note_autonomy", new string[] {"name"}),
        new Template("campaign_fact_note_abdicated", new string[] {"name"}),
        new Template("campaign_fact_note_usurp", new string[] {"name"}),
        new Template("campaign_fact_note_secede", new string[] {"name", "name"}),
        new Template("campaign_fact_note_exiled", new string[] {"name"}),
        new Template("campaign_fact_note_fixed_66", new string[] {}),
        new Template("campaign_fact_note_fixed_67", new string[] {}),
        new Template("campaign_fact_note_fixed_72", new string[] {}),
    };
    private static readonly Template[] Catalog = {
        new Template("campaign_fact_catalog_CW_Catalog_Demand_make_peace", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_Demand_continue_war", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_Demand_revoke_policy", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_Demand_redress", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Demand_autonomy", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Demand_usurp", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_make_peace", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_continue_war", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_revoke_policy", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_redress", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_autonomy", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_usurp", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_FactionName_default", new string[] {"name"}),
        new Template("campaign_fact_catalog_CW_Catalog_Outcome_rebels_usurp", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Outcome_rebels_secede", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Outcome_rebels_coerce", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Outcome_negotiated", new string[] {}),
        new Template("campaign_fact_catalog_CW_Catalog_Outcome_crown_victory", new string[] {}),
    };
    private static readonly Template ExecutionSentence = new Template("campaign_fact_execution_sentence", new string[] { "name", "name", "method" });
    private static readonly Template ExecutionQuote = new Template("campaign_fact_execution_quote", new string[] { "name", "quote" });
    private static readonly string[] Methods = { PromptComposerTexts.Raw("campaign_fact_method_0_source"), PromptComposerTexts.Raw("campaign_fact_method_1_source"), PromptComposerTexts.Raw("campaign_fact_method_2_source"), PromptComposerTexts.Raw("campaign_fact_method_3_source"), PromptComposerTexts.Raw("campaign_fact_method_4_source"), PromptComposerTexts.Raw("campaign_fact_method_5_source"), PromptComposerTexts.Raw("campaign_fact_method_6_source") };
    internal static bool IsSupported(string kind) => kind == "civil_war" || kind == "civil_war_politics" || kind == "civil_war_resolution" || kind == "execution_last_words";
    internal static string Project(string kind, string raw)
    {
        if (string.IsNullOrEmpty(raw) || !IsSupported(kind) || PromptComposerTexts.Get("campaign_fact_language") == "zh-CN") return raw;
        if (kind == "execution_last_words") return ProjectExecution(raw);
        string body=Apply(Civil,raw);
        if (body != raw) return body;
        string separator=PromptComposerTexts.Raw("campaign_fact_prefix_separator");
        int colon=raw.IndexOf(separator,StringComparison.Ordinal);
        // The bulletin producer prefixes one kingdom name. Reject ambiguous separators.
        if (colon <= 0 || raw.IndexOf(separator,colon+separator.Length,StringComparison.Ordinal)>=0) return raw;
        string candidate=raw.Substring(colon+separator.Length), projected=Apply(Civil,candidate);
        return candidate==projected ? raw : raw.Substring(0,colon)+": "+projected;
    }
    private static readonly Template[] Kill = {
        new Template("campaign_kill_murder",new string[]{"name"}),
        new Template("campaign_kill_labor",new string[]{"name"}),
        new Template("campaign_kill_oldage",new string[]{"name"}),
        new Template("campaign_kill_battle",new string[]{"name"}),
        new Template("campaign_kill_wounded",new string[]{"name"}),
        new Template("campaign_kill_execute",new string[]{"name"}),
        new Template("campaign_kill_death",new string[]{"name"}),
        new Template("campaign_kill_victim_after",new string[]{"name"}),
        new Template("campaign_kill_victim",new string[]{"name"}),
    };
    private static readonly Template[] PublicKill = {
        new Template("campaign_kill_public_0_1_1_1_personal",new string[]{"name","method","name","charge"}),
        new Template("campaign_kill_public_0_1_1_1_ordered",new string[]{"name","method","name","charge"}),
        new Template("campaign_kill_public_0_0_1_1_personal",new string[]{"method","name","charge"}),
        new Template("campaign_kill_public_0_0_1_1_ordered",new string[]{"method","name","charge"}),
        new Template("campaign_kill_public_0_1_0_1_personal",new string[]{"name","name","charge"}),
        new Template("campaign_kill_public_0_1_0_1_ordered",new string[]{"name","name","charge"}),
        new Template("campaign_kill_public_0_0_0_1_personal",new string[]{"name","charge"}),
        new Template("campaign_kill_public_0_0_0_1_ordered",new string[]{"name","charge"}),
        new Template("campaign_kill_public_1_1_1_1_known",new string[]{"name","name","method","charge"}),
        new Template("campaign_kill_public_1_1_1_1_unknown",new string[]{"name","method","charge"}),
        new Template("campaign_kill_public_1_0_1_1_known",new string[]{"name","method","charge"}),
        new Template("campaign_kill_public_1_0_1_1_unknown",new string[]{"method","charge"}),
        new Template("campaign_kill_public_1_1_0_1_known",new string[]{"name","name","charge"}),
        new Template("campaign_kill_public_1_1_0_1_unknown",new string[]{"name","charge"}),
        new Template("campaign_kill_public_0_1_1_0_personal",new string[]{"name","method","name"}),
        new Template("campaign_kill_public_0_1_1_0_ordered",new string[]{"name","method","name"}),
        new Template("campaign_kill_public_1_0_0_1_known",new string[]{"name","charge"}),
        new Template("campaign_kill_public_1_0_0_1_unknown",new string[]{"charge"}),
        new Template("campaign_kill_public_0_0_1_0_personal",new string[]{"method","name"}),
        new Template("campaign_kill_public_0_0_1_0_ordered",new string[]{"method","name"}),
        new Template("campaign_kill_public_0_1_0_0_personal",new string[]{"name","name"}),
        new Template("campaign_kill_public_0_1_0_0_ordered",new string[]{"name","name"}),
        new Template("campaign_kill_public_0_0_0_0_personal",new string[]{"name"}),
        new Template("campaign_kill_public_0_0_0_0_ordered",new string[]{"name"}),
        new Template("campaign_kill_public_1_1_1_0_known",new string[]{"name","name","method"}),
        new Template("campaign_kill_public_1_1_1_0_unknown",new string[]{"name","method"}),
        new Template("campaign_kill_public_1_0_1_0_known",new string[]{"name","method"}),
        new Template("campaign_kill_public_1_0_1_0_unknown",new string[]{"method"}),
        new Template("campaign_kill_public_1_1_0_0_known",new string[]{"name","name"}),
        new Template("campaign_kill_public_1_1_0_0_unknown",new string[]{"name"}),
        new Template("campaign_kill_public_1_0_0_0_known",new string[]{"name"}),
        new Template("campaign_kill_public_1_0_0_0_unknown",new string[]{}),
    };
    internal static string ProjectNpcKill(string kind,string raw,string actor)
    {
        if(kind!="hero_killed" && kind!="hero_executed_victim")return null;
        if(string.IsNullOrEmpty(raw)||string.IsNullOrEmpty(actor)||PromptComposerTexts.Get("campaign_fact_language")=="zh-CN")return null;
        foreach(string fixedKey in new[]{"campaign_kill_victim_unknown","campaign_kill_victim_after_unknown"})
            if(raw==PromptComposerTexts.Raw(fixedKey+"_source"))return Expand(fixedKey,new[]{actor});
        foreach(var template in PublicKill)
        {
            if(!Extract(raw,template.Parts,out string[] values))continue;
            for(int i=0;i<values.Length;i++)
            {
                if(template.Roles[i]=="method")for(int m=0;m<Methods.Length;m++)if(values[i]==Methods[m]){values[i]=PromptComposerTexts.Get("campaign_fact_method_"+m);break;}
                if(template.Roles[i]=="charge")for(int m=0;m<ExecutionValues.Length;m++)if(values[i]==ExecutionValues[m]){values[i]=PromptComposerTexts.Get("weekly_execution_value_"+m);break;}
            }
            var all=new string[values.Length+1];all[0]=actor;Array.Copy(values,0,all,1,values.Length);return Expand(template.Key,all);
        }
        foreach(var template in Kill)
            if(Extract(raw,template.Parts,out string[] values))return Expand(template.Key,new[]{actor,values[0]});
        return null;
    }
    private static readonly Template PersonalExecution = new Template("weekly_execution_actor_personal",new string[]{"name"});
    private static readonly Template OrderedExecution = new Template("weekly_execution_actor_ordered",new string[]{"name"});
    private static readonly string[] ExecutionValues = {PromptComposerTexts.Raw("weekly_execution_value_0_source"),PromptComposerTexts.Raw("weekly_execution_value_1_source"),PromptComposerTexts.Raw("weekly_execution_value_2_source"),PromptComposerTexts.Raw("weekly_execution_value_3_source"),PromptComposerTexts.Raw("weekly_execution_value_4_source"),PromptComposerTexts.Raw("weekly_execution_value_5_source"),PromptComposerTexts.Raw("weekly_execution_value_6_source"),PromptComposerTexts.Raw("weekly_execution_value_7_source"),PromptComposerTexts.Raw("weekly_execution_value_8_source"),PromptComposerTexts.Raw("weekly_execution_value_9_source"),PromptComposerTexts.Raw("weekly_execution_value_10_source"),PromptComposerTexts.Raw("weekly_execution_value_11_source")};
    internal static string ProjectWeeklyMaterial(string type, string key, string raw)
    {
        if (type == "noble_gathering" || (type == "raw_text" && (key ?? "").StartsWith("noble_gathering:", StringComparison.Ordinal)))
            return NobleGatheringTextProjection.ProjectWeeklyMaterialForPrompt(type, key, raw);
        if (type == "player_execution" || (type == "raw_text" && (key ?? "").StartsWith("player_execution:",StringComparison.Ordinal))) return ProjectPlayerExecution(raw);
        if (type == "raw_text")
        {
            if ((key ?? "").StartsWith("civil_war:",StringComparison.Ordinal)) return Project("civil_war",raw);
            if ((key ?? "").StartsWith("execution-last-words:",StringComparison.Ordinal)) return Project("execution_last_words",raw);
        }
        return Project(type,raw);
    }
    private static string ProjectPlayerExecution(string raw)
    {
        if (string.IsNullOrEmpty(raw) || PromptComposerTexts.Get("campaign_fact_language")=="zh-CN") return raw;
        int intro=raw.StartsWith(PromptComposerTexts.Raw("weekly_execution_raw_98"),StringComparison.Ordinal)?98:raw.StartsWith(PromptComposerTexts.Raw("weekly_execution_raw_99"),StringComparison.Ordinal)?99:0;
        if (intro==0) return raw;
        string period=PromptComposerTexts.Raw("weekly_execution_period");
        string tail=raw.Substring(PromptComposerTexts.Raw("weekly_execution_raw_"+intro).Length);
        string[] lines=tail.Split(new[]{period},StringSplitOptions.None);
        // This capture is ordered and complete. Truncated or unknown input stays original.
        if(lines.Length<4) return raw;
        var output=new System.Text.StringBuilder(PromptComposerTexts.Get("weekly_execution_fixed_"+intro));
        int[] labels={100,101,102,104,105,106,108,109,110,115,116,117,118,119};
        bool executor=false,victim=false,kind=false,constraint=false;
        int previous=-1;
        foreach(string line in lines)
        {
            if(line.Length==0)continue;
            if(line==PromptComposerTexts.Raw("weekly_execution_raw_113").TrimEnd(period.ToCharArray()) || line==PromptComposerTexts.Raw("weekly_execution_raw_123").TrimEnd(period.ToCharArray()))
            {
                if(constraint) return raw;constraint=true;
                output.Append(PromptComposerTexts.Get(line==PromptComposerTexts.Raw("weekly_execution_raw_113").TrimEnd(period.ToCharArray())?"weekly_execution_fixed_113":"weekly_execution_fixed_123"));continue;
            }
            if(constraint)return raw;
            if(line==PromptComposerTexts.Raw("weekly_execution_raw_114").TrimEnd(period.ToCharArray())) {kind=true;output.Append(PromptComposerTexts.Get("weekly_execution_fixed_114")).Append(' ');continue;}
            bool matched=false;
            for(int n=0;n<labels.Length;n++)
            {
                int id=labels[n];string prefix=PromptComposerTexts.Raw("weekly_execution_raw_"+id);
                if(!line.StartsWith(prefix,StringComparison.Ordinal))continue;
                if(n<=previous)return raw;previous=n;matched=true;
                string value=line.Substring(prefix.Length);if(value.Length==0)return raw;
                if(id==102)executor=true;if(id==106)victim=true;if(id==110)kind=true;
                if(id==110) {if(value==PromptComposerTexts.Raw("weekly_execution_raw_111"))value=PromptComposerTexts.Get("weekly_execution_fixed_111");else if(value==PromptComposerTexts.Raw("weekly_execution_raw_112"))value=PromptComposerTexts.Get("weekly_execution_fixed_112");}
                if(id==115)for(int m=0;m<Methods.Length;m++)if(value==Methods[m]){value=PromptComposerTexts.Get("campaign_fact_method_"+m);break;}
                if(id==116 || id==117 || id==118) for(int m=0;m<ExecutionValues.Length;m++)if(value==ExecutionValues[m]){value=PromptComposerTexts.Get("weekly_execution_value_"+m);break;}
                if(id==119) {if(Extract(value,PersonalExecution.Parts,out string[] who))value=Expand(PersonalExecution.Key,who);else if(Extract(value,OrderedExecution.Parts,out who))value=Expand(OrderedExecution.Key,who);}
                output.Append(PromptComposerTexts.Get("weekly_execution_label_"+id)).Append(value).Append(". ");break;
            }
            if(!matched)return raw;
        }
        return executor&&victim&&kind&&constraint?output.ToString().TrimEnd():raw;
    }
    private static string ProjectExecution(string raw)
    {
        string quoteStart=ExecutionQuote.Parts[0];
        int quote=raw.IndexOf(quoteStart,StringComparison.Ordinal);
        string sentence=quote<0?raw:raw.Substring(0,quote);
        string projectedSentence=sentence;
        if (Extract(sentence,ExecutionSentence.Parts,out string[] values))
        {
            for(int i=0;i<Methods.Length;i++) if(values[2]==Methods[i]) {values[2]=PromptComposerTexts.Get("campaign_fact_method_"+i);break;}
            projectedSentence=Expand("campaign_fact_execution_sentence",values);
        }
        if(quote<0) return projectedSentence;
        string quoted=raw.Substring(quote);
        // Quotation/body fields are inserted once, never searched or translated.
        if (!Extract(quoted,ExecutionQuote.Parts,out string[] speech)) return raw;
        return projectedSentence+Expand("campaign_fact_execution_quote",speech);
    }
    private static string Apply(Template[] templates,string raw)
    {
        // Fixed complete sentences outrank variable grammars that share a suffix.
        foreach(var template in templates)
            if(template.Parts.Length == 1 && raw == template.Parts[0]) return Expand(template.Key,Array.Empty<string>());
        foreach(var template in templates)
        {
            if(!Extract(raw,template.Parts,out string[] values)) continue;
            for(int i=0;i<values.Length;i++)
            {
                string role=template.Roles[i];
                if(role=="faction") values[i]=values[i]==PromptComposerTexts.Raw("campaign_fact_crown_source")?PromptComposerTexts.Get("campaign_fact_crown"):Apply(Catalog,values[i]);
                else if(role=="demand" || role=="outcome") values[i]=Apply(Catalog,values[i]);
                else if(role=="notes") values[i]=NoteBlocks.Replace(values[i],m=>Apply(Notes,m.Value));
            }
            return Expand(template.Key,values);
        }
        return raw;
    }
    private static string Expand(string key,string[] values) => Slots.Replace(PromptComposerTexts.Get(key),m=>values[int.Parse(m.Groups[1].Value)]);
    private static bool Extract(string raw,string[] parts,out string[] values)
    {
        values=null;
        if(parts.Length==1) {if(raw!=parts[0])return false;values=Array.Empty<string>();return true;}
        if(!raw.StartsWith(parts[0],StringComparison.Ordinal) || !raw.EndsWith(parts[parts.Length-1],StringComparison.Ordinal))return false;
        int position=parts[0].Length, limit=raw.Length-parts[parts.Length-1].Length;
        var result=new string[parts.Length-1];
        for(int i=0;i<result.Length;i++)
        {
            int at=i==result.Length-1?limit:raw.IndexOf(parts[i+1],position,StringComparison.Ordinal);
            if(at<position || at>limit)return false;
            if(i<result.Length-1 && raw.IndexOf(parts[i+1],at+parts[i+1].Length,StringComparison.Ordinal)>=0)return false;
            result[i]=raw.Substring(position,at-position);
            if(result[i].Length==0)return false;
            position=at+parts[i+1].Length;
        }
        if(position!=raw.Length)return false;
        values=result;return true;
    }
}
