import type { Player } from "@/types";

/**
 * "Guess the Player" mode data. Names and career club history are public
 * factual information (not a licensing concern) — see
 * docs/ASSETS_AND_RIGHTS.md. No player photo is used anywhere: the mode
 * shows a generic silhouette instead, so the guess comes entirely from
 * recognizing the career path (rendered with the same placeholder crests
 * as the logo mode). Every `careerClubIds` entry must exist in
 * src/data/clubs.ts.
 */
export const PLAYERS: readonly Player[] = [
  { id: "cristiano-ronaldo", name: "Cristiano Ronaldo", difficulty: 1, careerClubIds: ["sporting-cp", "man-utd", "real-madrid", "juventus", "al-nassr"] },
  { id: "lionel-messi", name: "Lionel Messi", difficulty: 1, careerClubIds: ["barcelona", "psg"] },
  { id: "kylian-mbappe", name: "Kylian Mbappe", difficulty: 1, careerClubIds: ["monaco", "psg", "real-madrid"] },
  { id: "erling-haaland", name: "Erling Haaland", difficulty: 1, careerClubIds: ["dortmund", "man-city"] },
  { id: "neymar", name: "Neymar", difficulty: 2, careerClubIds: ["barcelona", "psg", "al-hilal"] },
  { id: "luis-suarez", name: "Luis Suarez", difficulty: 2, careerClubIds: ["ajax", "liverpool", "barcelona", "atletico-madrid"] },
  { id: "robert-lewandowski", name: "Robert Lewandowski", difficulty: 2, careerClubIds: ["dortmund", "bayern-munich", "barcelona"] },
  { id: "zinedine-zidane", name: "Zinedine Zidane", difficulty: 2, careerClubIds: ["juventus", "real-madrid"] },
  { id: "thierry-henry", name: "Thierry Henry", difficulty: 2, careerClubIds: ["arsenal", "barcelona"] },
  { id: "david-beckham", name: "David Beckham", difficulty: 2, careerClubIds: ["man-utd", "real-madrid", "la-galaxy"] },
  { id: "sergio-ramos", name: "Sergio Ramos", difficulty: 2, careerClubIds: ["sevilla", "real-madrid", "psg"] },
  { id: "luka-modric", name: "Luka Modric", difficulty: 2, careerClubIds: ["tottenham", "real-madrid"] },
  { id: "zlatan-ibrahimovic", name: "Zlatan Ibrahimovic", difficulty: 2, careerClubIds: ["ajax", "juventus", "inter-milan", "barcelona", "ac-milan"] },
  { id: "ronaldinho", name: "Ronaldinho", difficulty: 3, careerClubIds: ["psg", "barcelona", "ac-milan"] },
  { id: "andrea-pirlo", name: "Andrea Pirlo", difficulty: 3, careerClubIds: ["inter-milan", "ac-milan", "juventus"] },
  { id: "didier-drogba", name: "Didier Drogba", difficulty: 3, careerClubIds: ["marseille", "chelsea"] },
  { id: "samuel-etoo", name: "Samuel Eto'o", difficulty: 3, careerClubIds: ["real-madrid", "barcelona", "inter-milan"] },
  { id: "xabi-alonso", name: "Xabi Alonso", difficulty: 3, careerClubIds: ["real-sociedad", "liverpool", "real-madrid", "bayern-munich"] },
  { id: "angel-di-maria", name: "Angel Di Maria", difficulty: 3, careerClubIds: ["benfica", "real-madrid", "psg", "juventus"] },
  { id: "gonzalo-higuain", name: "Gonzalo Higuain", difficulty: 3, careerClubIds: ["river-plate", "real-madrid", "napoli", "juventus"] },
  { id: "carlos-tevez", name: "Carlos Tevez", difficulty: 3, careerClubIds: ["boca-juniors", "man-utd", "man-city", "juventus"] },
  { id: "ronaldo-nazario", name: "Ronaldo Nazario", difficulty: 3, careerClubIds: ["psv", "barcelona", "inter-milan", "real-madrid", "ac-milan"] },
  { id: "radamel-falcao", name: "Radamel Falcao", difficulty: 3, careerClubIds: ["river-plate", "atletico-madrid", "monaco"] },
  { id: "javier-mascherano", name: "Javier Mascherano", difficulty: 4, careerClubIds: ["river-plate", "liverpool", "barcelona"] },
  { id: "diego-forlan", name: "Diego Forlan", difficulty: 4, careerClubIds: ["man-utd", "villarreal", "atletico-madrid"] },
];

export function getPlayerById(id: string): Player | undefined {
  return PLAYERS.find((p) => p.id === id);
}
