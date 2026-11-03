using Lexora.Domain.Entities;
using Lexora.Domain.Enums;

namespace Lexora.Application.Services;

public class RoadmapEngine
{
    private const double CanvasStartX = 180.0;
    private const double CanvasStepY = 130.0;
    private const double CanvasWaveAmplitude = 220.0;

    public StudyRoadmap GenerateAdaptiveRoadmap(
        string title,
        ExamTarget target,
        double startingScore,
        double targetScore,
        int durationWeeks)
    {
        var roadmap = new StudyRoadmap
        {
            Title = string.IsNullOrWhiteSpace(title) ? GetDefaultTitle(target, targetScore) : title,
            Target = target,
            StartingScore = startingScore,
            TargetScore = targetScore,
            TargetDate = DateTime.UtcNow.AddDays(Math.Max(14, durationWeeks * 7)),
            IsActive = true
        };

        var milestoneTemplates = GetMilestoneTemplates(target, startingScore, targetScore);
        int milestoneIndex = 0;
        int globalNodeIndex = 0;
        double currentY = 80.0;

        foreach (var mTemplate in milestoneTemplates)
        {
            var milestone = new RoadmapMilestone
            {
                RoadmapId = roadmap.Id,
                Title = mTemplate.Title,
                Description = mTemplate.Description,
                OrderIndex = milestoneIndex++,
                TargetCompletionDate = DateTime.UtcNow.AddDays((milestoneIndex * durationWeeks * 7.0) / milestoneTemplates.Count),
                IsCompleted = false
            };

            int nodeIndexInMilestone = 0;
            foreach (var nTemplate in mTemplate.Nodes)
            {
                // S-Curve Sine Wave Positioning for smooth Bézier splines
                double t = globalNodeIndex * 0.75;
                double canvasX = CanvasStartX + (Math.Sin(t) * CanvasWaveAmplitude);
                double canvasY = currentY;
                currentY += CanvasStepY;

                var node = new RoadmapNode
                {
                    MilestoneId = milestone.Id,
                    Title = nTemplate.Title,
                    Description = nTemplate.Description,
                    Type = nTemplate.Type,
                    Status = globalNodeIndex == 0 ? NodeStatus.Available : NodeStatus.Locked,
                    OrderIndex = nodeIndexInMilestone++,
                    CanvasX = Math.Round(canvasX, 1),
                    CanvasY = Math.Round(canvasY, 1),
                    TargetItemCount = nTemplate.TargetItemCount,
                    CompletedItemCount = 0,
                    EaseFactorWeight = CalculateInitialEaseFactor(targetScore - startingScore)
                };

                milestone.Nodes.Add(node);
                globalNodeIndex++;
            }

            roadmap.Milestones.Add(milestone);
        }

        return roadmap;
    }

    private static string GetDefaultTitle(ExamTarget target, double targetScore) => target switch
    {
        ExamTarget.Toeic => $"TOEIC Goal {targetScore:F0} Intensive Pathway",
        ExamTarget.Ielts => $"IELTS Target Band {targetScore:F1} Mastery Pathway",
        ExamTarget.Vstep => $"VSTEP Level B2/C1 Adaptive Pathway",
        _ => "Personalized Learning Roadmap"
    };

    private static double CalculateInitialEaseFactor(double scoreGap)
    {
        // Dynamic SM-2 initial Ease Factor (EF) adjustment based on difficulty gap
        // Standard floor is 1.3, default base is 2.5
        if (scoreGap > 300) return 2.1; // Harder gradient
        if (scoreGap > 150) return 2.3;
        return 2.5;
    }

    private sealed record MilestoneTemplate(string Title, string Description, IReadOnlyList<NodeTemplate> Nodes);
    private sealed record NodeTemplate(string Title, string Description, RoadmapNodeType Type, int TargetItemCount);

    private static IReadOnlyList<MilestoneTemplate> GetMilestoneTemplates(ExamTarget target, double startScore, double targetScore)
    {
        return target switch
        {
            ExamTarget.Toeic => GetToeicMilestones(startScore, targetScore),
            ExamTarget.Ielts => GetIeltsMilestones(startScore, targetScore),
            ExamTarget.Vstep => GetVstepMilestones(startScore, targetScore),
            _ => GetToeicMilestones(startScore, targetScore)
        };
    }

    private static IReadOnlyList<MilestoneTemplate> GetToeicMilestones(double startScore, double targetScore)
    {
        return
        [
            new MilestoneTemplate("Phase 1: High-Frequency Business Foundation", "Master core business, office operations, and contract vocabulary.",
            [
                new NodeTemplate("Office & Corporate Operations", "Corporate communication, departments, meetings", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Business Collocations & Phrasal Verbs", "Common verb-noun pairings in corporate contexts", RoadmapNodeType.VocabularySet, 25),
                new NodeTemplate("Subject-Verb Agreement & Tenses", "Part 5 core grammar mastery", RoadmapNodeType.GrammarPoint, 20),
                new NodeTemplate("Foundational Checkpoint Quiz", "Verify retention of Phase 1 concepts", RoadmapNodeType.Checkpoint, 15)
            ]),
            new MilestoneTemplate("Phase 2: Commercial, Financial & Travel Terminology", "Expand to purchasing, banking, customer service, and logistics.",
            [
                new NodeTemplate("Financial Reports & Invoicing", "Budgeting, accounting statements, billing terms", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Logistics, Shipping & Travel", "Itineraries, cargo handling, flight delays", RoadmapNodeType.VocabularySet, 25),
                new NodeTemplate("Participle Clauses & Conditionals", "Advanced modifiers used in contracts and notices", RoadmapNodeType.GrammarPoint, 20),
                new NodeTemplate("Mid-Term Mock Speed Test", "Timed mini-test simulating Parts 5 & 6", RoadmapNodeType.MockTest, 30)
            ]),
            new MilestoneTemplate("Phase 3: Paraphrasing & Speed Comprehension", "Bridge the gap to 800+ score ceiling with high-speed recognition.",
            [
                new NodeTemplate("Synonym Clusters & Subtle Nuances", "Trap words and semantic shifts in Part 7", RoadmapNodeType.VocabularySet, 35),
                new NodeTemplate("Inversion & Subjunctive Grammar", "Formal correspondence and policy announcements", RoadmapNodeType.GrammarPoint, 15),
                new NodeTemplate("Full-Length TOEIC Simulation", "Comprehensive 100-question speed and accuracy test", RoadmapNodeType.MockTest, 100)
            ])
        ];
    }

    private static IReadOnlyList<MilestoneTemplate> GetIeltsMilestones(double startScore, double targetScore)
    {
        return
        [
            new MilestoneTemplate("Phase 1: Academic Word List (AWL) & Syntax", "Establish Band 6.0 lexical base with formal academic terminology.",
            [
                new NodeTemplate("Academic Word List Sublist 1-3", "Abstract concepts, methodology, data discussion", RoadmapNodeType.VocabularySet, 35),
                new NodeTemplate("Complex Sentences & Relative Clauses", "Syntactic variety for Writing Task 1 & 2", RoadmapNodeType.GrammarPoint, 20),
                new NodeTemplate("Trend & Data Description Collocations", "Increase, plummet, oscillate, plateau terms", RoadmapNodeType.VocabularySet, 25),
                new NodeTemplate("Academic Diagnostic Checkpoint", "Review retention of AWL sets", RoadmapNodeType.Checkpoint, 20)
            ]),
            new MilestoneTemplate("Phase 2: Thematic Collocations & Essay Discourse", "Target Band 7.0+ across Environment, Society, and Technology.",
            [
                new NodeTemplate("Environment & Urbanization Lexis", "Sustainability, carbon offsets, habitat loss", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Passive Voice & Impersonal Structures", "Objective academic stance in research writing", RoadmapNodeType.GrammarPoint, 15),
                new NodeTemplate("Education, Tech & Artificial Intelligence", "Digital divide, pedagogy, automation impact", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Writing & Reading Speed Assessment", "Timed academic passage analysis", RoadmapNodeType.MockTest, 40)
            ]),
            new MilestoneTemplate("Phase 3: Idiomatic Precision & C1/C2 Fluency", "Reach Band 8.0+ through natural phrasing and lexical flexibility.",
            [
                new NodeTemplate("High-Band Idiomatic Collocations", "Natural native expressions without informal slang", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Cohesive Devices & Discourse Flow", "Furthermore, conversely, in stark contrast", RoadmapNodeType.GrammarPoint, 15),
                new NodeTemplate("Full Academic Mock Simulation", "Simulated multi-module test scenario", RoadmapNodeType.MockTest, 40)
            ])
        ];
    }

    private static IReadOnlyList<MilestoneTemplate> GetVstepMilestones(double startScore, double targetScore)
    {
        return
        [
            new MilestoneTemplate("Phase 1: VSTEP B1-B2 Core Communication", "Master standard Vietnamese national standardized exam themes.",
            [
                new NodeTemplate("Social Interaction & Daily Life", "Holidays, family, workplace etiquette", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Tense Consistency & Modal Verbs", "Core structures for VSTEP Writing Task 1 letters", RoadmapNodeType.GrammarPoint, 20),
                new NodeTemplate("Letter & Email Discourse Phrases", "Salutations, formal requests, apologies", RoadmapNodeType.VocabularySet, 25),
                new NodeTemplate("B1-B2 Checkpoint Assessment", "Verification of core communicative competency", RoadmapNodeType.Checkpoint, 20)
            ]),
            new MilestoneTemplate("Phase 2: VSTEP B2-C1 Essay & Academic Reading", "Transition to argumentative essays and formal reports.",
            [
                new NodeTemplate("Argumentative & Opinion Vocabulary", "Pros/cons, controversy, public policy", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("Complex Connectors & Inversions", "Structures for achieving C1 in Task 2 essay", RoadmapNodeType.GrammarPoint, 18),
                new NodeTemplate("Science, Health & Education Lexis", "Reading passage vocabulary across key genres", RoadmapNodeType.VocabularySet, 30),
                new NodeTemplate("VSTEP Full Reading & Writing Mock", "Timed exam simulation", RoadmapNodeType.MockTest, 40)
            ])
        ];
    }
}

