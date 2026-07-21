/** A real TypeScript enum, hence the .enum.ts suffix. Customer search only. */
export enum PhoneMatch {
  Prefix = 1,
  Suffix = 2
}

export const phoneMatchApiValue: Record<PhoneMatch, 'prefix' | 'suffix'> = {
  [PhoneMatch.Prefix]: 'prefix',
  [PhoneMatch.Suffix]: 'suffix'
};
