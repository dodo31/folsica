using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacementController : MonoBehaviour
{
	public GridController Grid;

	public Transform Environement;

	private BuildingController HeldBuilding;

	private Vector3 deltaFromObject;

	private bool isMovingObject;

	private Dictionary<BuildingController, Vector3> buildingGridPositions;

	protected void Start()
	{
		deltaFromObject = Vector3.zero;
		isMovingObject = false;

		Grid.gameObject.SetActive(false);

		buildingGridPositions = new Dictionary<BuildingController, Vector3>();
	}

	public void AddBuilding(GameObject buildingPrefab)
	{
		Grid.gameObject.SetActive(true);

		GameObject newBuilding = Instantiate<GameObject>(buildingPrefab);
		Vector3 spawnScreenPosition = Input.mousePosition - new Vector3(0, 20, 0);
		newBuilding.transform.position = this.PointedPosition(spawnScreenPosition);
		newBuilding.transform.SetParent(Environement);

		BuildingController newBuildingController = newBuilding.GetComponent<BuildingController>();
		newBuildingController.HighlightAsNeutral();

		this.StartMove(newBuilding);
	}

	public void RemoveBuilding(BuildingController building)
	{
		this.DestroyBuilding(building);
	}

	public void StartMove(GameObject objectToMove)
	{
		HeldBuilding = objectToMove.GetComponent<BuildingController>();

		Vector3 objectScreenPosition = Camera.main.WorldToScreenPoint(HeldBuilding.transform.position);
		Vector3 mouseScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
		deltaFromObject = objectScreenPosition - mouseScreenPosition;

		Grid.gameObject.SetActive(true);

		isMovingObject = true;
	}

	public void refreshPosition()
	{
		Vector3 mouseScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y);
		Vector3 newObjectScreenPosition = mouseScreenPosition + deltaFromObject;

		Vector3 newObjectPosition = this.PointedPosition(newObjectScreenPosition);

		if (newObjectPosition.x != float.NegativeInfinity)
		{
			Vector3 buildingGridPosition = Grid.FreePositionToGridPosition(newObjectPosition);
			HeldBuilding.transform.position = buildingGridPosition;

			if (this.IsBuildingColliding(HeldBuilding))
			{
				HeldBuilding.HighlightAsInvalid();
			}
			else
			{
				HeldBuilding.HighlightAsNeutral();
			}
		}
	}

	public void EndMove()
	{
		Vector3 objectGridPosition = Grid.FreePositionToGridPosition(HeldBuilding.transform.position);

		if (!this.IsBuildingColliding(HeldBuilding))
		{
			this.PlaceBuilding(objectGridPosition);
		}
		else
		{
			this.CancelPlacement();
		}

		Grid.gameObject.SetActive(false);
		isMovingObject = false;
	}

	private void PlaceBuilding(Vector3 objectGridPosition)
	{
		if (!buildingGridPositions.ContainsKey(HeldBuilding))
		{
			buildingGridPositions.Add(HeldBuilding, objectGridPosition);
		}
		else
		{
			buildingGridPositions[HeldBuilding] = objectGridPosition;
		}
	}

	public void CancelPlacement()
	{
		if (buildingGridPositions.ContainsKey(HeldBuilding))
		{
			HeldBuilding.transform.position = buildingGridPositions[HeldBuilding];
		}
		else
		{
			this.DestroyBuilding(HeldBuilding);
		}
	}

	private void DestroyBuilding(BuildingController building)
	{
		DestroyImmediate(building.gameObject);
		HeldBuilding = null;
	}

	private Vector3 PointedPosition(Vector3 screenPosition)
	{
		Ray ray = Camera.main.ScreenPointToRay(screenPosition);
		RaycastHit[] hits = Physics.RaycastAll(ray);

		if (hits.Length > 0)
		{
			RaycastHit matchingHit = Array.Find(hits, (RaycastHit hit) =>
				hit.transform == Grid.transform
			);

			if (matchingHit.transform != null)
			{
				return matchingHit.point;
			}

			return Vector3.negativeInfinity;
		}
		else
		{
			return Vector3.negativeInfinity;
		}
	}

	public bool IsMovingObject()
	{
		return isMovingObject;
	}

	private bool IsBuildingColliding(BuildingController building)
	{
		bool isColliding = false;

		IEnumerator<BuildingController> placedBuildings = buildingGridPositions.Keys.GetEnumerator();

		while (placedBuildings.MoveNext() && !isColliding)
		{
			BuildingController placedBuilding = placedBuildings.Current;
			Vector3 buildingGridPosition = buildingGridPositions[placedBuilding];

			if (building != placedBuilding)
			{
				Vector3 positionOther = building.transform.position;
				int rowCountOther = building.RowCount;
				int colCountOther = building.ColCount;
				BuildingFootprintRow[] buildingFootprintRowsOther = building.FootprintRows;

				Vector3 positionPlaced = placedBuilding.transform.position;
				int rowCountPlaced = placedBuilding.RowCount;
				int colCountPlaced = placedBuilding.ColCount;
				BuildingFootprintRow[] buildingFootprintRowsPlaced = placedBuilding.FootprintRows;

				for (int rowPlaced = 0; rowPlaced < rowCountPlaced; rowPlaced++)
				{
					for (int colPlaced = 0; colPlaced < colCountPlaced; colPlaced++)
					{
						bool isFilledPlaced = buildingFootprintRowsPlaced[rowPlaced].cells[colPlaced];

						if (isFilledPlaced)
						{
							Vector3 cellPositionPlaced = positionPlaced + new Vector3(colPlaced, 0, -rowPlaced);

							for (int rowOther = 0; rowOther < rowCountOther; rowOther++)
							{
								for (int colOther = 0; colOther < colCountOther; colOther++)
								{
									bool isFilledOther = buildingFootprintRowsOther[rowOther].cells[colOther];

									if (isFilledOther)
									{
										Vector3 cellPositionOther = positionOther + new Vector3(colOther, 0, -rowOther);

										if (cellPositionOther.Equals(cellPositionPlaced))
										{
											isColliding = true;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		return isColliding;
	}
}
