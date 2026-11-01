import { orderedDoses, sameDose } from './dose';

describe('orderedDoses', () => {
  it('RN10: lists the primary doses in order, then the boosters', () => {
    expect(orderedDoses({ primaryDoses: 2, boosterDoses: 2 })).toEqual([
      { type: 'Primary', number: 1 },
      { type: 'Primary', number: 2 },
      { type: 'Booster', number: 1 },
      { type: 'Booster', number: 2 },
    ]);
  });

  it('RN09: has no booster in a scheme without boosters', () => {
    expect(orderedDoses({ primaryDoses: 1, boosterDoses: 0 })).toEqual([
      { type: 'Primary', number: 1 },
    ]);
  });
});

describe('sameDose', () => {
  it('RN08: identifies a dose by the type and the number', () => {
    expect(sameDose({ type: 'Primary', number: 1 }, { type: 'Primary', number: 1 })).toBe(true);
    expect(sameDose({ type: 'Primary', number: 1 }, { type: 'Booster', number: 1 })).toBe(false);
    expect(sameDose({ type: 'Primary', number: 1 }, { type: 'Primary', number: 2 })).toBe(false);
  });

  it('does not match a dose with the absent next dose of a complete scheme', () => {
    expect(sameDose({ type: 'Primary', number: 1 }, null)).toBe(false);
  });
});
