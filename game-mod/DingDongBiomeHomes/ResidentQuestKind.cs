using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;

namespace DingDongBiomeHomes;
internal enum ResidentQuestKind : byte
{
	SurveyOrigin,
	PatrolOrigin,
	DeliverSupplies,
	DefendOrigin,
	BossHunt,
	BiomePurge,
	BannerCollection,
	ModProgression,
	CreatureHunt,
	VisitBiome,
	// First Word Was authored forms (QUEST-FORMS.md). Appended only: the byte
	// value is persisted in the world ledger and the quest snapshot.
	Observe,
	GatherEnv,
	Trial,
	DialogueChoice,
	Delayed,
	Expedition
}
