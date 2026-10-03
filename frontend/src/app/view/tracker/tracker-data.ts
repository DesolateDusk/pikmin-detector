export type PikminColor = 'red' | 'yellow' | 'blue' | 'purple' | 'white' | 'rock' | 'winged' | 'ice';
export type CollectionStatus = 'collected' | 'missing';
export type PageId = 'overview' | 'tracking' | 'nearby';

export interface ColorOption {
  id: PikminColor;
  label: string;
  hex: string;
}

export interface RecognizedPikminResponse {
  pikminType: PikminColor;
  status: CollectionStatus;
}

export interface RecognizedCostumeResponse {
  costumeTypeKey: string;
  costumeTypeName: string;
  availableTypes: RecognizedPikminResponse[];
}

export interface RecognizedSeriesResponse {
  decorTypeKey: string;
  decorTypeName: string;
  costumes: RecognizedCostumeResponse[];
}

export interface RecognitionResponse {
  series: RecognizedSeriesResponse[];
}

export interface SpotResponse {
  id: string;
  name: string;
  country: string | null;
  city: string | null;
  area: string | null;
  latitude: number;
  longitude: number;
  decorTypes: { key: string; name: Record<string, string> }[];
  distanceMeters: number | null;
}

export const COLORS: readonly ColorOption[] = [
  { id: 'red', label: '紅色', hex: '#eb6e63' },
  { id: 'yellow', label: '黃色', hex: '#f2c653' },
  { id: 'blue', label: '藍色', hex: '#70a9d8' },
  { id: 'purple', label: '紫色', hex: '#a88dbd' },
  { id: 'white', label: '白色', hex: '#e7e9e5' },
  { id: 'rock', label: '岩石', hex: '#8e9698' },
  { id: 'winged', label: '羽翅', hex: '#e5a4bb' },
  { id: 'ice', label: '冰', hex: '#9bd6db' },
];

export function entryId(decorTypeKey: string, costumeTypeKey: string, pikminType: PikminColor): string {
  return `${decorTypeKey}/${costumeTypeKey}/${pikminType}`;
}

export function mergeRecognition(
  current: readonly RecognizedSeriesResponse[],
  incoming: readonly RecognizedSeriesResponse[],
): RecognizedSeriesResponse[] {
  const merged = structuredClone(current) as RecognizedSeriesResponse[];
  for (const series of incoming) {
    let storedSeries = merged.find((item) => item.decorTypeKey === series.decorTypeKey);
    if (!storedSeries) {
      storedSeries = { decorTypeKey: series.decorTypeKey, decorTypeName: series.decorTypeName, costumes: [] };
      merged.push(storedSeries);
    }
    storedSeries.decorTypeName = series.decorTypeName;
    for (const costume of series.costumes) {
      let storedCostume = storedSeries.costumes.find((item) => item.costumeTypeKey === costume.costumeTypeKey);
      if (!storedCostume) {
        storedCostume = { costumeTypeKey: costume.costumeTypeKey, costumeTypeName: costume.costumeTypeName, availableTypes: [] };
        storedSeries.costumes.push(storedCostume);
      }
      storedCostume.costumeTypeName = costume.costumeTypeName;
      for (const type of costume.availableTypes) {
        const index = storedCostume.availableTypes.findIndex((item) => item.pikminType === type.pikminType);
        if (index === -1) storedCostume.availableTypes.push({ ...type });
        else storedCostume.availableTypes[index] = { ...type };
      }
    }
  }
  return merged;
}
